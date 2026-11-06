using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Point de passage unique de toute variation de stock.
///
/// Aucun autre service ne touche à une quantité : c'est ce qui garantit que
/// chaque mouvement laisse une trace, avec le stock avant et après, et que
/// l'on peut toujours expliquer un écart d'inventaire.
///
/// Le stock ne devient pas négatif : vendre une pièce que l'on n'a pas est
/// presque toujours une erreur de saisie, et la refuser au moment où elle se
/// produit coûte bien moins cher que de la découvrir au comptage.
/// </summary>
public class InventaireService : IInventaireService
{
    /// <summary>Réglage autorisant, à titre exceptionnel, un stock négatif.</summary>
    public const string CleStockNegatif = "stock.autoriser.negatif";

    private readonly IApplicationDbContext _context;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public InventaireService(
        IApplicationDbContext context,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IAuditService audit)
    {
        _context = context;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    /// <summary>
    /// Enregistre un mouvement et met à jour la quantité du produit.
    /// L'enregistrement en base est laissé à l'appelant, afin qu'une vente et
    /// ses mouvements de stock soient écrits dans la même transaction.
    /// </summary>
    public async Task<InventoryTransaction> EnregistrerAsync(
        MouvementStockRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.Quantite == 0)
        {
            throw new RegleMetierException("La quantité d'un mouvement de stock ne peut pas être nulle.");
        }

        var produit = await _context.Products
                          .FirstOrDefaultAsync(p => p.Id == requete.ProduitId, cancellationToken)
                      ?? throw IntrouvableException.Pour("Produit", requete.ProduitId);

        var stockAvant = produit.CurrentStock;
        var stockApres = stockAvant + requete.Quantite;

        // Une pièce unique n'existe qu'une fois : même quand l'atelier
        // accepte de vendre ce qui n'est pas encore en stock, elle ne peut
        // pas être vendue deux fois.
        if (produit.IsUniquePiece && stockApres < 0)
        {
            throw new RegleMetierException(
                $"« {produit.Name} » est une pièce unique et elle n'est plus disponible " +
                "(déjà vendue ou sortie du stock).");
        }

        if (stockApres < 0 && !requete.AutoriserStockNegatif
            && !await StockNegatifAutoriseAsync(cancellationToken))
        {
            throw new RegleMetierException(
                $"Stock insuffisant pour « {produit.Name} ». " +
                $"Disponible : {Formatage.Quantite(stockAvant)} · " +
                $"Demandé : {Formatage.Quantite(Math.Abs(requete.Quantite))}.");
        }

        if (produit.IsUniquePiece && stockApres > 1)
        {
            throw new RegleMetierException(
                $"« {produit.Name} » est une pièce unique : elle ne peut pas compter plus d'un exemplaire " +
                $"en stock (il y en a déjà {Formatage.Quantite(stockAvant)}).");
        }

        produit.CurrentStock = stockApres;

        // Le coût de revient suit la moyenne pondérée : une entrée au prix du
        // jour ne doit pas effacer ce que les pièces déjà en stock ont coûté.
        if (requete.Quantite > 0 && requete.CoutUnitaire > 0)
        {
            var valeurAvant = stockAvant * produit.ProductionCost;
            var valeurEntree = requete.Quantite * requete.CoutUnitaire;

            produit.ProductionCost = stockApres > 0
                ? Math.Round((valeurAvant + valeurEntree) / stockApres, 2)
                : requete.CoutUnitaire;
        }

        var mouvement = new InventoryTransaction
        {
            TransactionType = requete.TypeMouvement,
            ProductId = produit.Id,
            Quantity = requete.Quantite,
            QuantityBefore = stockAvant,
            QuantityAfter = stockApres,
            UnitCost = requete.CoutUnitaire,
            TotalCost = Math.Round(Math.Abs(requete.Quantite) * requete.CoutUnitaire, 2),
            OccurredAt = requete.Date ?? _horloge.MaintenantUtc,
            SaleId = requete.VenteId,
            CustomOrderId = requete.CommandeId,
            ReversedTransactionId = requete.MouvementAnnuleId,
            Reference = requete.Reference,
            Notes = requete.Notes,
            UserId = _utilisateurCourant.UtilisateurId
        };

        _context.InventoryTransactions.Add(mouvement);
        return mouvement;
    }

    /// <summary>
    /// Annule les mouvements d'un document en créant les mouvements inverses,
    /// plutôt qu'en effaçant les originaux : l'historique garde la trace de
    /// ce qui a eu lieu, et de son annulation.
    /// </summary>
    public async Task<IReadOnlyList<InventoryTransaction>> AnnulerDocumentAsync(
        int? venteId, int? commandeId, string motif, CancellationToken cancellationToken = default)
    {
        var mouvements = await _context.InventoryTransactions
            .Where(t =>
                (venteId != null && t.SaleId == venteId) ||
                (commandeId != null && t.CustomOrderId == commandeId))
            .Where(t => t.TransactionType != InventoryTransactionType.Annulation)
            .ToListAsync(cancellationToken);

        // Un mouvement déjà annulé ne doit pas l'être une seconde fois.
        var dejaAnnules = await _context.InventoryTransactions
            .Where(t => t.ReversedTransactionId != null)
            .Select(t => t.ReversedTransactionId!.Value)
            .ToListAsync(cancellationToken);

        var inverses = new List<InventoryTransaction>();

        foreach (var origine in mouvements.Where(m => !dejaAnnules.Contains(m.Id)))
        {
            inverses.Add(await EnregistrerAsync(new MouvementStockRequete
            {
                TypeMouvement = InventoryTransactionType.Annulation,
                ProduitId = origine.ProductId,
                Quantite = -origine.Quantity,
                CoutUnitaire = origine.UnitCost,
                VenteId = origine.SaleId,
                CommandeId = origine.CustomOrderId,
                MouvementAnnuleId = origine.Id,
                Reference = origine.Reference,
                Notes = motif,
                // L'inversion doit toujours aboutir, même si elle ramène le
                // stock à zéro : refuser laisserait le document à moitié annulé.
                AutoriserStockNegatif = true
            }, cancellationToken));
        }

        return inverses;
    }

    public async Task<PagedResult<MouvementStockDto>> ListerAsync(
        FiltreMouvementsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.InventoryTransactions
            .Include(t => t.Product)
            .Include(t => t.User)
            .AsNoTracking()
            .AsQueryable();

        if (requete.TypeMouvement is not null)
        {
            requeteBase = requeteBase.Where(t => t.TransactionType == requete.TypeMouvement);
        }

        if (requete.ProduitId is not null)
        {
            requeteBase = requeteBase.Where(t => t.ProductId == requete.ProduitId);
        }

        if (requete.Du is not null)
        {
            var du = DateTime.SpecifyKind(requete.Du.Value.Date, DateTimeKind.Utc);
            requeteBase = requeteBase.Where(t => t.OccurredAt >= du);
        }

        if (requete.Au is not null)
        {
            var au = DateTime.SpecifyKind(requete.Au.Value.Date.AddDays(1), DateTimeKind.Utc);
            requeteBase = requeteBase.Where(t => t.OccurredAt < au);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(t =>
                t.Product.Name.ToLower().Contains(recherche)
                || t.Product.Reference.ToLower().Contains(recherche)
                || (t.Reference != null && t.Reference.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var lignes = await requeteBase
            .OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        var elements = lignes.Select(t => new MouvementStockDto(
            t.Id,
            _horloge.VersHeureAtelier(t.OccurredAt),
            t.TransactionType.Libelle(),
            t.Product.Name,
            t.Product.Reference,
            t.Quantity,
            t.QuantityBefore,
            t.QuantityAfter,
            t.UnitCost,
            t.TotalCost,
            t.Reference,
            t.User?.FullName,
            t.Notes)).ToList();

        return new PagedResult<MouvementStockDto>(elements, total, requete.Page, requete.TaillePage);
    }

    /// <summary>
    /// Entrée en stock ou casse saisie à la main. Une casse doit être
    /// expliquée : des pièces qui disparaissent sans motif ne s'analysent pas.
    /// </summary>
    public Task<MouvementStockDto> EnregistrerManuelAsync(
        MouvementManuelRequete requete, CancellationToken cancellationToken = default)
        => _context.EnTransactionAsync(() => EnregistrerSaisieAsync(requete, cancellationToken), cancellationToken);

    private async Task<MouvementStockDto> EnregistrerSaisieAsync(
        MouvementManuelRequete requete, CancellationToken cancellationToken)
    {
        if (requete.TypeMouvement is not (InventoryTransactionType.Entree or InventoryTransactionType.Perte))
        {
            throw new RegleMetierException(
                "Seules une entrée en stock ou une casse se saisissent ici. Un retour client " +
                "passe par l'annulation de la vente, et un écart de comptage par la correction du stock.");
        }

        if (requete.Quantite <= 0)
        {
            throw new RegleMetierException("Indiquez un nombre de pièces supérieur à zéro.");
        }

        if (requete.CoutUnitaire < 0)
        {
            throw new RegleMetierException("Le coût d'une pièce ne peut pas être négatif.");
        }

        var perte = requete.TypeMouvement == InventoryTransactionType.Perte;

        if (perte && string.IsNullOrWhiteSpace(requete.Notes))
        {
            throw new RegleMetierException(
                "Expliquez la casse ou la perte (pièce fêlée à la cuisson, tombée, abîmée…) : " +
                "un écart de stock non expliqué ne s'analyse plus ensuite.");
        }

        var produit = await _context.Products.AsNoTracking()
                          .FirstOrDefaultAsync(p => p.Id == requete.ProduitId, cancellationToken)
                      ?? throw IntrouvableException.Pour("Produit", requete.ProduitId);

        await EnregistrerAsync(new MouvementStockRequete
        {
            TypeMouvement = requete.TypeMouvement,
            ProduitId = produit.Id,
            Quantite = perte ? -requete.Quantite : requete.Quantite,
            CoutUnitaire = perte || requete.CoutUnitaire == 0 ? produit.ProductionCost : requete.CoutUnitaire,
            Reference = perte ? "Casse ou perte" : "Entrée en stock",
            Notes = string.IsNullOrWhiteSpace(requete.Notes) ? null : requete.Notes.Trim()
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(
            AuditAction.Modification, "Stock", produit.Id.ToString(),
            $"{(perte ? "Casse ou perte" : "Entrée en stock")} : {Formatage.Quantite(requete.Quantite)} × " +
            $"« {produit.Name} ».",
            cancellationToken: cancellationToken);

        return (await ListerAsync(
                new FiltreMouvementsRequete { ProduitId = produit.Id, TaillePage = 1 },
                cancellationToken))
            .Elements.First();
    }

    /// <summary>
    /// Enregistre une régularisation après comptage physique. Le motif est
    /// obligatoire : un écart corrigé sans explication ne s'analyse plus.
    /// </summary>
    public Task<MouvementStockDto> RegulariserAsync(
        RegularisationRequete requete, CancellationToken cancellationToken = default)
        => _context.EnTransactionAsync(() => CorrigerAsync(requete, cancellationToken), cancellationToken);

    private async Task<MouvementStockDto> CorrigerAsync(
        RegularisationRequete requete, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(requete.Motif))
        {
            throw new RegleMetierException(
                "Indiquez le motif de la régularisation : casse, perte, erreur de comptage…");
        }

        if (requete.QuantiteComptee < 0)
        {
            throw new RegleMetierException("La quantité comptée ne peut pas être négative.");
        }

        var produit = await _context.Products
                          .FirstOrDefaultAsync(p => p.Id == requete.ProduitId, cancellationToken)
                      ?? throw IntrouvableException.Pour("Produit", requete.ProduitId);

        var ecart = requete.QuantiteComptee - produit.CurrentStock;

        if (ecart == 0)
        {
            throw new RegleMetierException(
                $"Le stock de « {produit.Name} » correspond déjà au comptage : " +
                $"{Formatage.Quantite(produit.CurrentStock)}.");
        }

        var mouvement = await EnregistrerAsync(new MouvementStockRequete
        {
            TypeMouvement = InventoryTransactionType.Regularisation,
            ProduitId = produit.Id,
            Quantite = ecart,
            CoutUnitaire = produit.ProductionCost,
            Reference = requete.Motif.Trim(),
            Notes = requete.Notes,
            AutoriserStockNegatif = true
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(
            AuditAction.Modification, "Stock", produit.Id.ToString(),
            $"Régularisation de « {produit.Name} » : {Formatage.Quantite(ecart)} " +
            $"({requete.Motif.Trim()}).",
            cancellationToken: cancellationToken);

        return (await ListerAsync(
                new FiltreMouvementsRequete { ProduitId = produit.Id, TaillePage = 1 },
                cancellationToken))
            .Elements.First();
    }

    /// <summary>
    /// Indique si l'atelier accepte un stock négatif. Déconseillé, mais
    /// parfois nécessaire le temps de rattraper une saisie en retard.
    /// </summary>
    public async Task<bool> StockNegatifAutoriseAsync(CancellationToken cancellationToken = default)
    {
        var reglage = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == CleStockNegatif, cancellationToken);

        return reglage is not null
               && bool.TryParse(reglage.Value, out var autorise)
               && autorise;
    }
}
