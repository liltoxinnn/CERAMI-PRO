using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Entities.Purchasing;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Achats de matières premières : saisie, confirmation, réception et annulation.
/// Le stock n'augmente qu'à la réception, matière par matière, avec création
/// d'un lot permettant de retrouver le coût réel (flux « achat de matières »).
/// </summary>
public class AchatService : IAchatService
{
    private readonly IApplicationDbContext _context;
    private readonly IInventaireService _inventaire;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public AchatService(
        IApplicationDbContext context,
        IInventaireService inventaire,
        IReferenceNumberService numerotation,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IAuditService audit)
    {
        _context = context;
        _inventaire = inventaire;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<AchatDto>> ListerAsync(
        FiltreAchatsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.FournisseurId is not null)
        {
            requeteBase = requeteBase.Where(a => a.SupplierId == requete.FournisseurId);
        }

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(a => a.Status == requete.Statut);
        }

        if (requete.SeulementImpayes)
        {
            requeteBase = requeteBase.Where(a => a.PaidAmount < a.TotalAmount && a.Status != PurchaseStatus.Annule);
        }

        if (requete.Du is not null)
        {
            var du = DateTime.SpecifyKind(requete.Du.Value.Date, DateTimeKind.Utc);
            requeteBase = requeteBase.Where(a => a.PurchaseDate >= du);
        }

        if (requete.Au is not null)
        {
            var au = DateTime.SpecifyKind(requete.Au.Value.Date.AddDays(1), DateTimeKind.Utc);
            requeteBase = requeteBase.Where(a => a.PurchaseDate < au);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(a =>
                a.PurchaseNumber.ToLower().Contains(recherche) ||
                a.Supplier.Name.ToLower().Contains(recherche) ||
                (a.InvoiceReference != null && a.InvoiceReference.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var achats = await requeteBase
            .OrderByDescending(a => a.PurchaseDate).ThenByDescending(a => a.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<AchatDto>(
            achats.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<AchatDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var achat = await ChargerAvecDetails().AsNoTracking()
                        .FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Achat", id);

        return Convertir(achat);
    }

    public async Task<AchatDto> CreerAsync(AchatRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierRequeteAsync(requete, cancellationToken);

        var achat = new Purchase
        {
            PurchaseNumber = await _numerotation.GenererAsync(TypeDocument.Achat, cancellationToken),
            SupplierId = requete.FournisseurId,
            PurchaseDate = requete.Date ?? _horloge.MaintenantUtc,
            Status = PurchaseStatus.Brouillon,
            DiscountAmount = requete.Remise,
            ShippingCost = requete.FraisLivraison,
            InvoiceReference = Nettoyer(requete.ReferenceFacture),
            Notes = Nettoyer(requete.Notes),
            UserId = _utilisateurCourant.UtilisateurId
        };

        RemplirLignes(achat, requete);
        Recalculer(achat);

        _context.Purchases.Add(achat);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Purchase), achat.Id.ToString(),
            $"Création de l'achat {achat.PurchaseNumber} " +
            $"({Formatage.Montant(achat.TotalAmount)}).", null, cancellationToken);

        return await ObtenirAsync(achat.Id, cancellationToken);
    }

    public async Task<AchatDto> ModifierAsync(
        int id, AchatRequete requete, CancellationToken cancellationToken = default)
    {
        var achat = await ChargerAvecDetails().FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Achat", id);

        if (achat.Status != PurchaseStatus.Brouillon)
        {
            throw new RegleMetierException(
                $"L'achat {achat.PurchaseNumber} est « {achat.Status.Libelle()} » : " +
                "seul un brouillon peut être modifié.");
        }

        await VerifierRequeteAsync(requete, cancellationToken);

        _context.PurchaseItems.RemoveRange(achat.Items);
        achat.Items.Clear();

        achat.SupplierId = requete.FournisseurId;
        achat.PurchaseDate = requete.Date ?? achat.PurchaseDate;
        achat.DiscountAmount = requete.Remise;
        achat.ShippingCost = requete.FraisLivraison;
        achat.InvoiceReference = Nettoyer(requete.ReferenceFacture);
        achat.Notes = Nettoyer(requete.Notes);

        RemplirLignes(achat, requete);
        Recalculer(achat);

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Purchase), id.ToString(),
            $"Modification de l'achat {achat.PurchaseNumber}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<AchatDto> ConfirmerAsync(int id, CancellationToken cancellationToken = default)
    {
        var achat = await ChargerAvecDetails().FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Achat", id);

        if (achat.Status != PurchaseStatus.Brouillon)
        {
            throw new RegleMetierException($"L'achat {achat.PurchaseNumber} est déjà confirmé.");
        }

        if (achat.Items.Count == 0)
        {
            throw new RegleMetierException("Ajoutez au moins une matière avant de confirmer l'achat.");
        }

        achat.Status = PurchaseStatus.Confirme;
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Purchase), id.ToString(),
            $"Confirmation de l'achat {achat.PurchaseNumber}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<AchatDto> ReceptionnerAsync(
        int id, ReceptionAchatRequete requete, CancellationToken cancellationToken = default)
    {
        var achat = await ChargerAvecDetails().FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Achat", id);

        if (achat.Status is PurchaseStatus.Brouillon)
        {
            throw new RegleMetierException("Confirmez l'achat avant d'enregistrer une réception.");
        }

        if (achat.Status is PurchaseStatus.Annule or PurchaseStatus.Recu)
        {
            throw new RegleMetierException(
                $"L'achat {achat.PurchaseNumber} est « {achat.Status.Libelle()} » : aucune réception n'est possible.");
        }

        if (requete.Lignes.Count == 0)
        {
            throw new RegleMetierException("Indiquez les quantités reçues.");
        }

        foreach (var ligneRecue in requete.Lignes.Where(l => l.QuantiteRecue > 0))
        {
            var ligne = achat.Items.FirstOrDefault(i => i.Id == ligneRecue.LigneAchatId)
                        ?? throw new RegleMetierException("Une ligne de réception ne correspond à aucune ligne d'achat.");

            var restant = ligne.Quantity - ligne.ReceivedQuantity;

            if (ligneRecue.QuantiteRecue > restant)
            {
                throw new RegleMetierException(
                    $"La quantité reçue pour « {ligne.Material.Name} » dépasse la quantité commandée. " +
                    $"Restant à recevoir : {Formatage.Quantite(restant, ligne.Unit.Code)}.");
            }

            var coutUnitaire = ligne.Quantity > 0
                ? Math.Round((ligne.LineTotal) / ligne.Quantity, 4)
                : ligne.UnitPrice;

            var lot = new MaterialBatch
            {
                BatchNumber = await _numerotation.GenererAsync(TypeDocument.LotMatiere, cancellationToken),
                MaterialId = ligne.MaterialId,
                PurchaseItemId = ligne.Id,
                Quantity = ligneRecue.QuantiteRecue,
                RemainingQuantity = ligneRecue.QuantiteRecue,
                UnitCost = coutUnitaire,
                ReceivedDate = _horloge.MaintenantUtc,
                ExpiryDate = ligneRecue.DatePeremption,
                Location = Nettoyer(ligneRecue.Emplacement),
                Notes = Nettoyer(requete.Notes)
            };

            _context.MaterialBatches.Add(lot);
            await _context.SaveChangesAsync(cancellationToken);

            await _inventaire.EnregistrerAsync(new MouvementStockRequete
            {
                TypeArticle = InventoryItemType.MatierePremiere,
                TypeMouvement = InventoryTransactionType.Achat,
                MatiereId = ligne.MaterialId,
                LotId = lot.Id,
                Quantite = ligneRecue.QuantiteRecue,
                CoutUnitaire = coutUnitaire,
                AchatId = achat.Id,
                Reference = achat.PurchaseNumber,
                Notes = Nettoyer(requete.Notes)
            }, cancellationToken);

            ligne.ReceivedQuantity += ligneRecue.QuantiteRecue;
        }

        achat.Status = achat.Items.All(i => i.ReceivedQuantity >= i.Quantity)
            ? PurchaseStatus.Recu
            : PurchaseStatus.PartiellementRecu;

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Purchase), id.ToString(),
            $"Réception de l'achat {achat.PurchaseNumber} : {achat.Status.Libelle()}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<AchatDto> AnnulerAsync(
        int id, string motif, CancellationToken cancellationToken = default)
    {
        var achat = await ChargerAvecDetails().FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Achat", id);

        if (achat.Status == PurchaseStatus.Annule)
        {
            throw new RegleMetierException($"L'achat {achat.PurchaseNumber} est déjà annulé.");
        }

        if (achat.PaidAmount > 0)
        {
            throw new RegleMetierException(
                $"L'achat {achat.PurchaseNumber} a déjà été réglé " +
                $"({Formatage.Montant(achat.PaidAmount)}). " +
                "Annulez d'abord les règlements correspondants.");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("Indiquez le motif de l'annulation.");
        }

        // Les quantités déjà reçues ressortent du stock (règle métier n°6).
        await _inventaire.AnnulerDocumentAsync(achat.Id, null, null,
            $"Annulation de l'achat {achat.PurchaseNumber} : {motif.Trim()}", cancellationToken);

        foreach (var ligne in achat.Items)
        {
            ligne.ReceivedQuantity = 0;
        }

        foreach (var lot in await _context.MaterialBatches
                     .Where(l => l.PurchaseItem != null && l.PurchaseItem.PurchaseId == achat.Id)
                     .ToListAsync(cancellationToken))
        {
            lot.RemainingQuantity = 0;
            lot.Notes = $"Lot annulé : {motif.Trim()}";
        }

        achat.Status = PurchaseStatus.Annule;
        achat.Notes = string.IsNullOrWhiteSpace(achat.Notes)
            ? $"Annulé : {motif.Trim()}"
            : $"{achat.Notes}\nAnnulé : {motif.Trim()}";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Annulation, nameof(Purchase), id.ToString(),
            $"Annulation de l'achat {achat.PurchaseNumber} : {motif.Trim()}", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    private IQueryable<Purchase> ChargerAvecDetails()
        => _context.Purchases
            .Include(a => a.Supplier)
            .Include(a => a.User)
            .Include(a => a.Items).ThenInclude(i => i.Material)
            .Include(a => a.Items).ThenInclude(i => i.Unit);

    private static void RemplirLignes(Purchase achat, AchatRequete requete)
    {
        foreach (var ligne in requete.Lignes)
        {
            achat.Items.Add(new PurchaseItem
            {
                MaterialId = ligne.MatiereId,
                UnitId = ligne.UniteId,
                Quantity = ligne.Quantite,
                UnitPrice = ligne.PrixUnitaire,
                DiscountAmount = ligne.Remise,
                LineTotal = Math.Round(ligne.Quantite * ligne.PrixUnitaire - ligne.Remise, 2),
                Notes = Nettoyer(ligne.Notes)
            });
        }
    }

    private static void Recalculer(Purchase achat)
    {
        achat.Subtotal = Math.Round(achat.Items.Sum(i => i.LineTotal), 2);
        achat.TotalAmount = Math.Round(achat.Subtotal - achat.DiscountAmount + achat.ShippingCost, 2);
    }

    private async Task VerifierRequeteAsync(AchatRequete requete, CancellationToken cancellationToken)
    {
        if (!await _context.Suppliers.AnyAsync(f => f.Id == requete.FournisseurId, cancellationToken))
        {
            throw new RegleMetierException("Le fournisseur sélectionné n'existe pas.");
        }

        if (requete.Lignes.Count == 0)
        {
            throw new RegleMetierException("Ajoutez au moins une matière à l'achat.");
        }

        foreach (var ligne in requete.Lignes)
        {
            if (ligne.Quantite <= 0)
            {
                throw new RegleMetierException("Chaque ligne doit avoir une quantité supérieure à zéro.");
            }

            if (ligne.PrixUnitaire < 0)
            {
                throw new RegleMetierException("Le prix unitaire ne peut pas être négatif.");
            }

            if (!await _context.Materials.AnyAsync(m => m.Id == ligne.MatiereId, cancellationToken))
            {
                throw new RegleMetierException("Une des matières sélectionnées n'existe pas.");
            }

            if (!await _context.Units.AnyAsync(u => u.Id == ligne.UniteId, cancellationToken))
            {
                throw new RegleMetierException("Une des unités sélectionnées n'existe pas.");
            }
        }

        if (requete.Remise < 0 || requete.FraisLivraison < 0)
        {
            throw new RegleMetierException("La remise et les frais de livraison ne peuvent pas être négatifs.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static AchatDto Convertir(Purchase a) => new(
        a.Id,
        a.PurchaseNumber,
        a.SupplierId,
        a.Supplier.Name,
        a.PurchaseDate,
        a.Status,
        a.Status.Libelle(),
        a.Subtotal,
        a.DiscountAmount,
        a.ShippingCost,
        a.TotalAmount,
        a.PaidAmount,
        a.RemainingAmount,
        a.InvoiceReference,
        a.Notes,
        a.User?.FullName,
        a.Items.Select(i => new LigneAchatDto(
            i.Id, i.MaterialId, i.Material.Name, i.Material.Reference,
            i.UnitId, i.Unit.Code, i.Quantity, i.ReceivedQuantity,
            i.UnitPrice, i.DiscountAmount, i.LineTotal, i.Notes)).ToList());
}
