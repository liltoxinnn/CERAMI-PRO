using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Gère toutes les entrées et sorties de stock, matières comme produits finis.
/// Chaque variation laisse une trace indiquant le stock avant et après (règle n°2)
/// et le stock ne peut pas devenir négatif sans dérogation (règle n°1).
/// </summary>
public class InventaireService : IInventaireService
{
    /// <summary>Réglage autorisant, à titre exceptionnel, un stock négatif.</summary>
    public const string CleStockNegatif = "stock.autoriser.negatif";

    private readonly IApplicationDbContext _context;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IReferenceNumberService _numerotation;
    private readonly IAuditService _audit;

    public InventaireService(
        IApplicationDbContext context,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IReferenceNumberService numerotation,
        IAuditService audit)
    {
        _context = context;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _numerotation = numerotation;
        _audit = audit;
    }

    public async Task<InventoryTransaction> EnregistrerAsync(
        MouvementStockRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.Quantite == 0)
        {
            throw new RegleMetierException("La quantité d'un mouvement de stock ne peut pas être nulle.");
        }

        var (nomArticle, stockAvant, appliquer) = await ChargerArticleAsync(requete, cancellationToken);
        var stockApres = stockAvant + requete.Quantite;

        if (stockApres < 0 && !requete.AutoriserStockNegatif && !await StockNegatifAutoriseAsync(cancellationToken))
        {
            throw new RegleMetierException(
                $"Stock insuffisant pour « {nomArticle} ». " +
                $"Disponible : {Formatage.Quantite(stockAvant)} · " +
                $"Demandé : {Formatage.Quantite(Math.Abs(requete.Quantite))}.");
        }

        appliquer(stockApres);

        var mouvement = new InventoryTransaction
        {
            ItemType = requete.TypeArticle,
            TransactionType = requete.TypeMouvement,
            MaterialId = requete.MatiereId,
            ProductId = requete.ProduitId,
            ProductVariantId = requete.VarianteId,
            MaterialBatchId = requete.LotId,
            Quantity = requete.Quantite,
            QuantityBefore = stockAvant,
            QuantityAfter = stockApres,
            UnitCost = requete.CoutUnitaire,
            TotalCost = Math.Round(Math.Abs(requete.Quantite) * requete.CoutUnitaire, 2),
            OccurredAt = requete.Date ?? _horloge.MaintenantUtc,
            PurchaseId = requete.AchatId,
            SaleId = requete.VenteId,
            ProductionOrderId = requete.ProductionId,
            StockAdjustmentId = requete.AjustementId,
            ReversedTransactionId = requete.MouvementAnnuleId,
            Reference = requete.Reference,
            Notes = requete.Notes,
            UserId = _utilisateurCourant.UtilisateurId
        };

        _context.InventoryTransactions.Add(mouvement);
        return mouvement;
    }

    /// <summary>
    /// Charge l'article concerné, renvoie son nom, son stock actuel et la façon
    /// d'y appliquer le nouveau stock. Le coût moyen est recalculé à la réception.
    /// </summary>
    private async Task<(string Nom, decimal Stock, Action<decimal> Appliquer)> ChargerArticleAsync(
        MouvementStockRequete requete, CancellationToken cancellationToken)
    {
        if (requete.TypeArticle == InventoryItemType.MatierePremiere)
        {
            if (requete.MatiereId is null)
            {
                throw new RegleMetierException("La matière première concernée par le mouvement est obligatoire.");
            }

            var matiere = await _context.Materials
                .Include(m => m.Unit)
                .FirstOrDefaultAsync(m => m.Id == requete.MatiereId, cancellationToken)
                ?? throw IntrouvableException.Pour("Matière première", requete.MatiereId);

            var stock = matiere.CurrentQuantity;

            return (matiere.Name, stock, nouveau =>
            {
                // À la réception, le coût moyen est recalculé au prorata des quantités.
                if (requete.Quantite > 0 && requete.CoutUnitaire > 0 && stock + requete.Quantite > 0)
                {
                    var valeurExistante = Math.Max(stock, 0) * matiere.AverageCost;
                    var valeurEntrante = requete.Quantite * requete.CoutUnitaire;
                    matiere.AverageCost = Math.Round(
                        (valeurExistante + valeurEntrante) / (Math.Max(stock, 0) + requete.Quantite), 4);
                }

                if (requete.TypeMouvement == InventoryTransactionType.Achat && requete.CoutUnitaire > 0)
                {
                    matiere.LastPurchasePrice = requete.CoutUnitaire;
                }

                matiere.CurrentQuantity = nouveau;
            }
            );
        }

        if (requete.VarianteId is not null)
        {
            var variante = await _context.ProductVariants
                .FirstOrDefaultAsync(v => v.Id == requete.VarianteId, cancellationToken)
                ?? throw IntrouvableException.Pour("Variante de produit", requete.VarianteId);

            return (variante.Name, variante.CurrentStock, nouveau => variante.CurrentStock = nouveau);
        }

        if (requete.ProduitId is null)
        {
            throw new RegleMetierException("Le produit concerné par le mouvement est obligatoire.");
        }

        var produit = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == requete.ProduitId, cancellationToken)
            ?? throw IntrouvableException.Pour("Produit", requete.ProduitId);

        return (produit.Name, produit.CurrentStock, nouveau => produit.CurrentStock = nouveau);
    }

    public async Task<IReadOnlyList<InventoryTransaction>> AnnulerDocumentAsync(
        int? achatId, int? venteId, int? productionId, string motif,
        CancellationToken cancellationToken = default)
    {
        var mouvements = await _context.InventoryTransactions
            .Where(t =>
                (achatId != null && t.PurchaseId == achatId) ||
                (venteId != null && t.SaleId == venteId) ||
                (productionId != null && t.ProductionOrderId == productionId))
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
                TypeArticle = origine.ItemType,
                TypeMouvement = InventoryTransactionType.Annulation,
                MatiereId = origine.MaterialId,
                ProduitId = origine.ProductId,
                VarianteId = origine.ProductVariantId,
                Quantite = -origine.Quantity,
                CoutUnitaire = origine.UnitCost,
                AchatId = origine.PurchaseId,
                VenteId = origine.SaleId,
                ProductionId = origine.ProductionOrderId,
                MouvementAnnuleId = origine.Id,
                Reference = origine.Reference,
                Notes = motif,
                // L'inversion doit toujours aboutir, même si elle ramène le stock à zéro.
                AutoriserStockNegatif = true
            }, cancellationToken));
        }

        return inverses;
    }

    public async Task<PagedResult<MouvementStockDto>> ListerAsync(
        FiltreMouvementsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.InventoryTransactions
            .Include(t => t.Material).ThenInclude(m => m!.Unit)
            .Include(t => t.Product)
            .Include(t => t.User)
            .AsNoTracking()
            .AsQueryable();

        if (requete.TypeArticle is not null)
        {
            requeteBase = requeteBase.Where(t => t.ItemType == requete.TypeArticle);
        }

        if (requete.TypeMouvement is not null)
        {
            requeteBase = requeteBase.Where(t => t.TransactionType == requete.TypeMouvement);
        }

        if (requete.MatiereId is not null)
        {
            requeteBase = requeteBase.Where(t => t.MaterialId == requete.MatiereId);
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
                (t.Material != null && (t.Material.Name.ToLower().Contains(recherche)
                                        || t.Material.Reference.ToLower().Contains(recherche))) ||
                (t.Product != null && (t.Product.Name.ToLower().Contains(recherche)
                                       || t.Product.Reference.ToLower().Contains(recherche))) ||
                (t.Reference != null && t.Reference.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var lignes = await requeteBase
            .OrderByDescending(t => t.OccurredAt).ThenByDescending(t => t.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        var elements = lignes.Select(t => new MouvementStockDto(
            t.Id,
            t.OccurredAt,
            t.ItemType.Libelle(),
            t.TransactionType.Libelle(),
            t.Material?.Name ?? t.Product?.Name ?? "Article supprimé",
            t.Material?.Reference ?? t.Product?.Reference,
            t.Material?.Unit.Code ?? "pièce",
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

    public async Task<MouvementStockDto> RegulariserAsync(
        RegularisationRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.QuantiteComptee < 0)
        {
            throw new RegleMetierException("La quantité comptée ne peut pas être négative.");
        }

        var stockActuel = requete.TypeArticle == InventoryItemType.MatierePremiere
            ? (await _context.Materials.FirstOrDefaultAsync(m => m.Id == requete.MatiereId, cancellationToken)
               ?? throw IntrouvableException.Pour("Matière première", requete.MatiereId ?? 0)).CurrentQuantity
            : (await _context.Products.FirstOrDefaultAsync(p => p.Id == requete.ProduitId, cancellationToken)
               ?? throw IntrouvableException.Pour("Produit", requete.ProduitId ?? 0)).CurrentStock;

        var ecart = requete.QuantiteComptee - stockActuel;

        if (ecart == 0)
        {
            throw new RegleMetierException(
                "La quantité comptée est identique au stock enregistré : aucune régularisation n'est nécessaire.");
        }

        var regularisation = new StockAdjustment
        {
            Reference = await _numerotation.GenererAsync(TypeDocument.Ajustement, cancellationToken),
            ItemType = requete.TypeArticle,
            MaterialId = requete.MatiereId,
            ProductId = requete.ProduitId,
            Reason = requete.Motif,
            QuantityBefore = stockActuel,
            CountedQuantity = requete.QuantiteComptee,
            Difference = ecart,
            AdjustmentDate = _horloge.MaintenantUtc,
            Notes = requete.Notes,
            UserId = _utilisateurCourant.UtilisateurId
        };

        _context.StockAdjustments.Add(regularisation);
        await _context.SaveChangesAsync(cancellationToken);

        var mouvement = await EnregistrerAsync(new MouvementStockRequete
        {
            TypeArticle = requete.TypeArticle,
            TypeMouvement = requete.Motif == StockAdjustmentReason.Casse
                            || requete.Motif == StockAdjustmentReason.Perte
                ? InventoryTransactionType.Endommage
                : InventoryTransactionType.Ajustement,
            MatiereId = requete.MatiereId,
            ProduitId = requete.ProduitId,
            Quantite = ecart,
            AjustementId = regularisation.Id,
            Reference = regularisation.Reference,
            Notes = requete.Notes ?? requete.Motif.Libelle(),
            AutoriserStockNegatif = true
        }, cancellationToken);

        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(StockAdjustment),
            regularisation.Id.ToString(),
            $"Régularisation {regularisation.Reference} : écart de {Formatage.Quantite(ecart)}.",
            null, cancellationToken);

        var page = await ListerAsync(new FiltreMouvementsRequete { TaillePage = 1 }, cancellationToken);
        return page.Elements.FirstOrDefault()
               ?? new MouvementStockDto(mouvement.Id, mouvement.OccurredAt,
                   mouvement.ItemType.Libelle(), mouvement.TransactionType.Libelle(),
                   string.Empty, null, string.Empty, mouvement.Quantity,
                   mouvement.QuantityBefore, mouvement.QuantityAfter, 0, 0,
                   regularisation.Reference, null, requete.Notes);
    }

    public async Task<bool> StockNegatifAutoriseAsync(CancellationToken cancellationToken = default)
    {
        var reglage = await _context.SystemSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Key == CleStockNegatif, cancellationToken);

        return bool.TryParse(reglage?.Value, out var autorise) && autorise;
    }
}
