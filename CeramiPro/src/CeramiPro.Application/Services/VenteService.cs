using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Invoicing;
using CeramiPro.Domain.Entities.Sales;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Ventes de produits finis. Une vente confirmée diminue le stock (règle n°3),
/// enregistre le mouvement correspondant, émet la facture et peut encaisser
/// immédiatement un règlement partiel ou complet.
/// </summary>
public class VenteService : IVenteService
{
    private readonly IApplicationDbContext _context;
    private readonly IInventaireService _inventaire;
    private readonly IPaiementService _paiements;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public VenteService(
        IApplicationDbContext context,
        IInventaireService inventaire,
        IPaiementService paiements,
        IReferenceNumberService numerotation,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IAuditService audit)
    {
        _context = context;
        _inventaire = inventaire;
        _paiements = paiements;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<VenteDto>> ListerAsync(
        FiltreVentesRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.ClientId is not null)
        {
            requeteBase = requeteBase.Where(v => v.CustomerId == requete.ClientId);
        }

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(v => v.Status == requete.Statut);
        }

        if (requete.SeulementImpayees)
        {
            requeteBase = requeteBase.Where(v => v.PaidAmount < v.TotalAmount && v.Status == SaleStatus.Confirmee);
        }

        if (requete.Du is not null)
        {
            var du = DateTime.SpecifyKind(requete.Du.Value.Date, DateTimeKind.Utc);
            requeteBase = requeteBase.Where(v => v.SaleDate >= du);
        }

        if (requete.Au is not null)
        {
            var au = DateTime.SpecifyKind(requete.Au.Value.Date.AddDays(1), DateTimeKind.Utc);
            requeteBase = requeteBase.Where(v => v.SaleDate < au);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(v =>
                v.SaleNumber.ToLower().Contains(recherche) ||
                (v.Customer != null && v.Customer.FullName.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var ventes = await requeteBase
            .OrderByDescending(v => v.SaleDate).ThenByDescending(v => v.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<VenteDto>(
            ventes.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<VenteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var vente = await ChargerAvecDetails().AsNoTracking()
                        .FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Vente", id);

        return Convertir(vente);
    }

    public async Task<VenteDto> EnregistrerAsync(
        VenteRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.Lignes.Count == 0)
        {
            throw new RegleMetierException("Ajoutez au moins un produit à la vente.");
        }

        if (requete.ClientId is not null
            && !await _context.Customers.AnyAsync(c => c.Id == requete.ClientId, cancellationToken))
        {
            throw new RegleMetierException("Le client sélectionné n'existe pas.");
        }

        var parametres = await _context.BusinessSettings.OrderBy(p => p.Id).FirstAsync(cancellationToken);

        var vente = new Sale
        {
            SaleNumber = await _numerotation.GenererAsync(TypeDocument.Vente, cancellationToken),
            CustomerId = requete.ClientId,
            SaleDate = requete.Date ?? _horloge.MaintenantUtc,
            Status = SaleStatus.Confirmee,
            DiscountAmount = requete.Remise,
            Notes = Nettoyer(requete.Notes),
            UserId = _utilisateurCourant.UtilisateurId
        };

        foreach (var ligne in requete.Lignes)
        {
            if (ligne.Quantite <= 0)
            {
                throw new RegleMetierException("Chaque ligne doit avoir une quantité supérieure à zéro.");
            }

            var produit = await _context.Products
                              .FirstOrDefaultAsync(p => p.Id == ligne.ProduitId, cancellationToken)
                          ?? throw new RegleMetierException("Un des produits sélectionnés n'existe pas.");

            var prix = ligne.PrixUnitaire > 0 ? ligne.PrixUnitaire : produit.SellingPrice;

            vente.Items.Add(new SaleItem
            {
                ProductId = produit.Id,
                ProductVariantId = ligne.VarianteId,
                Description = produit.Name,
                Quantity = ligne.Quantite,
                UnitPrice = prix,
                DiscountAmount = ligne.Remise,
                LineTotal = Math.Round(ligne.Quantite * prix - ligne.Remise, 2),
                UnitCost = produit.ProductionCost
            });
        }

        vente.Subtotal = Math.Round(vente.Items.Sum(i => i.LineTotal), 2);
        vente.TaxAmount = parametres.TaxEnabled
            ? Math.Round((vente.Subtotal - vente.DiscountAmount) * parametres.DefaultTaxRate / 100m, 2)
            : 0m;
        vente.TotalAmount = Math.Round(vente.Subtotal - vente.DiscountAmount + vente.TaxAmount, 2);
        vente.TotalCost = Math.Round(vente.Items.Sum(i => i.Quantity * i.UnitCost), 2);

        if (requete.MontantPaye > vente.TotalAmount)
        {
            throw new RegleMetierException(
                $"Le montant encaissé dépasse le total de la vente " +
                $"({Formatage.Montant(vente.TotalAmount)}).");
        }

        _context.Sales.Add(vente);
        await _context.SaveChangesAsync(cancellationToken);

        // Le stock des produits finis diminue, mouvement par mouvement.
        foreach (var ligne in vente.Items)
        {
            await _inventaire.EnregistrerAsync(new MouvementStockRequete
            {
                TypeArticle = InventoryItemType.ProduitFini,
                TypeMouvement = InventoryTransactionType.Vente,
                ProduitId = ligne.ProductId,
                VarianteId = ligne.ProductVariantId,
                Quantite = -ligne.Quantity,
                CoutUnitaire = ligne.UnitCost,
                VenteId = vente.Id,
                Reference = vente.SaleNumber
            }, cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        if (requete.EmettreFacture)
        {
            await CreerFactureAsync(vente, parametres.DefaultTaxRate, cancellationToken);
        }

        if (requete.MontantPaye > 0)
        {
            if (requete.ModeReglementId is null)
            {
                throw new RegleMetierException("Choisissez le mode de règlement.");
            }

            await _paiements.EnregistrerAsync(new PaiementRequete
            {
                ClientId = vente.CustomerId,
                VenteId = vente.Id,
                Montant = requete.MontantPaye,
                ModeReglementId = requete.ModeReglementId.Value,
                Date = vente.SaleDate
            }, cancellationToken);
        }

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Sale), vente.Id.ToString(),
            $"Vente {vente.SaleNumber} de {Formatage.Montant(vente.TotalAmount)}.",
            null, cancellationToken);

        return await ObtenirAsync(vente.Id, cancellationToken);
    }

    /// <summary>Crée la facture correspondant à la vente.</summary>
    private async Task CreerFactureAsync(Sale vente, decimal tauxTva, CancellationToken cancellationToken)
    {
        var facture = new Invoice
        {
            InvoiceNumber = await _numerotation.GenererAsync(TypeDocument.Facture, cancellationToken),
            CustomerId = vente.CustomerId,
            SaleId = vente.Id,
            IssueDate = vente.SaleDate,
            Subtotal = vente.Subtotal,
            DiscountAmount = vente.DiscountAmount,
            TaxRate = vente.TaxAmount > 0 ? tauxTva : 0m,
            TaxAmount = vente.TaxAmount,
            TotalAmount = vente.TotalAmount,
            Status = InvoiceStatus.Emise,
            UserId = _utilisateurCourant.UtilisateurId
        };

        foreach (var ligne in vente.Items)
        {
            facture.Items.Add(new InvoiceItem
            {
                ProductId = ligne.ProductId,
                Description = ligne.Description,
                Quantity = ligne.Quantity,
                UnitPrice = ligne.UnitPrice,
                DiscountAmount = ligne.DiscountAmount,
                LineTotal = ligne.LineTotal
            });
        }

        _context.Invoices.Add(facture);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<VenteDto> AnnulerAsync(
        int id, string motif, CancellationToken cancellationToken = default)
    {
        var vente = await ChargerAvecDetails().FirstOrDefaultAsync(v => v.Id == id, cancellationToken)
                    ?? throw IntrouvableException.Pour("Vente", id);

        if (vente.Status == SaleStatus.Annulee)
        {
            throw new RegleMetierException($"La vente {vente.SaleNumber} est déjà annulée.");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("Indiquez le motif de l'annulation.");
        }

        if (vente.PaidAmount > 0)
        {
            throw new RegleMetierException(
                $"La vente {vente.SaleNumber} a déjà été réglée " +
                $"({Formatage.Montant(vente.PaidAmount)}). Annulez d'abord les paiements.");
        }

        // Les produits reviennent en stock (règle métier n°6).
        await _inventaire.AnnulerDocumentAsync(null, vente.Id, null,
            $"Annulation de la vente {vente.SaleNumber} : {motif.Trim()}", cancellationToken);

        foreach (var facture in await _context.Invoices
                     .Where(f => f.SaleId == vente.Id).ToListAsync(cancellationToken))
        {
            facture.Status = InvoiceStatus.Annulee;
        }

        vente.Status = SaleStatus.Annulee;
        vente.Notes = string.IsNullOrWhiteSpace(vente.Notes)
            ? $"Annulée : {motif.Trim()}"
            : $"{vente.Notes}\nAnnulée : {motif.Trim()}";

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Annulation, nameof(Sale), id.ToString(),
            $"Annulation de la vente {vente.SaleNumber} : {motif.Trim()}", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    private IQueryable<Sale> ChargerAvecDetails()
        => _context.Sales
            .Include(v => v.Customer)
            .Include(v => v.User)
            .Include(v => v.Invoices)
            .Include(v => v.Items).ThenInclude(i => i.Product);

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static VenteDto Convertir(Sale v) => new(
        v.Id,
        v.SaleNumber,
        v.CustomerId,
        v.Customer?.FullName ?? "Client de passage",
        v.SaleDate,
        v.Status,
        v.Status.Libelle(),
        v.Subtotal,
        v.DiscountAmount,
        v.TaxAmount,
        v.TotalAmount,
        v.PaidAmount,
        v.RemainingAmount,
        v.TotalCost,
        Math.Round(v.TotalAmount - v.TaxAmount - v.TotalCost, 2),
        v.Notes,
        v.User?.FullName,
        v.Invoices.FirstOrDefault(f => f.Status != InvoiceStatus.Annulee)?.InvoiceNumber,
        v.Items.Select(i => new LigneVenteDto(
            i.Id, i.ProductId, i.Product.Name, i.Product.Reference, i.ProductVariantId,
            i.Description, i.Quantity, i.UnitPrice, i.DiscountAmount, i.LineTotal)).ToList());
}
