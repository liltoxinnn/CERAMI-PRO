using System.Linq.Expressions;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Fiches des matières premières et consommables. La quantité en stock n'est
/// jamais modifiée directement : elle passe toujours par un mouvement (règle n°2).
/// </summary>
public class MatiereService : IMatiereService
{
    private readonly IApplicationDbContext _context;
    private readonly IInventaireService _inventaire;
    private readonly IReferenceNumberService _numerotation;
    private readonly IAuditService _audit;

    public MatiereService(
        IApplicationDbContext context,
        IInventaireService inventaire,
        IReferenceNumberService numerotation,
        IAuditService audit)
    {
        _context = context;
        _inventaire = inventaire;
        _numerotation = numerotation;
        _audit = audit;
    }

    public async Task<PagedResult<MatiereDto>> ListerAsync(
        FiltreMatieresRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.Materials
            .Include(m => m.MaterialCategory)
            .Include(m => m.Unit)
            .Include(m => m.Supplier)
            .AsNoTracking()
            .AsQueryable();

        if (!requete.InclureInactives)
        {
            requeteBase = requeteBase.Where(m => m.IsActive);
        }

        if (requete.CategorieId is not null)
        {
            requeteBase = requeteBase.Where(m => m.MaterialCategoryId == requete.CategorieId);
        }

        if (requete.FournisseurId is not null)
        {
            requeteBase = requeteBase.Where(m => m.SupplierId == requete.FournisseurId);
        }

        if (requete.SeulementStockFaible)
        {
            requeteBase = requeteBase.Where(m => m.CurrentQuantity <= m.MinimumStock);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(m =>
                m.Name.ToLower().Contains(recherche) ||
                m.Reference.ToLower().Contains(recherche) ||
                (m.Location != null && m.Location.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var elements = await requeteBase
            .OrderBy(m => m.Name)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<MatiereDto>(elements, total, requete.Page, requete.TaillePage);
    }

    public async Task<MatiereDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Materials
               .Include(m => m.MaterialCategory).Include(m => m.Unit).Include(m => m.Supplier)
               .AsNoTracking()
               .Where(m => m.Id == id)
               .Select(Projection)
               .FirstOrDefaultAsync(cancellationToken)
           ?? throw IntrouvableException.Pour("Matière première", id);

    public async Task<MatiereDto> CreerAsync(
        MatiereRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierReferencesAsync(requete, cancellationToken);

        var matiere = new Material
        {
            Reference = await _numerotation.GenererAsync(TypeDocument.Matiere, cancellationToken),
            Name = requete.Nom.Trim(),
            MaterialCategoryId = requete.CategorieId,
            UnitId = requete.UniteId,
            MinimumStock = requete.StockMinimum,
            MaximumStock = requete.StockMaximum,
            LastPurchasePrice = requete.PrixAchat,
            AverageCost = requete.PrixAchat,
            SupplierId = requete.FournisseurId,
            Location = Nettoyer(requete.Emplacement),
            Description = Nettoyer(requete.Description),
            ImagePath = Nettoyer(requete.Image),
            IsActive = requete.Actif
        };

        _context.Materials.Add(matiere);
        await _context.SaveChangesAsync(cancellationToken);

        // Le stock de départ entre par un mouvement, comme toute autre quantité.
        if (requete.StockInitial > 0)
        {
            await _inventaire.EnregistrerAsync(new MouvementStockRequete
            {
                TypeArticle = InventoryItemType.MatierePremiere,
                TypeMouvement = InventoryTransactionType.Ajustement,
                MatiereId = matiere.Id,
                Quantite = requete.StockInitial,
                CoutUnitaire = requete.PrixAchat,
                Reference = matiere.Reference,
                Notes = "Stock présent dans l'atelier à la création de la fiche."
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Material), matiere.Id.ToString(),
            $"Création de la matière « {matiere.Name} » ({matiere.Reference}).", null, cancellationToken);

        return await ObtenirAsync(matiere.Id, cancellationToken);
    }

    public async Task<MatiereDto> ModifierAsync(
        int id, MatiereRequete requete, CancellationToken cancellationToken = default)
    {
        var matiere = await _context.Materials.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Matière première", id);

        await VerifierReferencesAsync(requete, cancellationToken);

        matiere.Name = requete.Nom.Trim();
        matiere.MaterialCategoryId = requete.CategorieId;
        matiere.UnitId = requete.UniteId;
        matiere.MinimumStock = requete.StockMinimum;
        matiere.MaximumStock = requete.StockMaximum;
        matiere.LastPurchasePrice = requete.PrixAchat;
        matiere.SupplierId = requete.FournisseurId;
        matiere.Location = Nettoyer(requete.Emplacement);
        matiere.Description = Nettoyer(requete.Description);
        matiere.ImagePath = Nettoyer(requete.Image);
        matiere.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Material), id.ToString(),
            $"Modification de la matière « {matiere.Name} ».", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var matiere = await _context.Materials.FirstOrDefaultAsync(m => m.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Matière première", id);

        var utilisee = await _context.InventoryTransactions.AnyAsync(t => t.MaterialId == id, cancellationToken)
                       || await _context.ProductRecipeItems.AnyAsync(i => i.MaterialId == id, cancellationToken)
                       || await _context.PurchaseItems.AnyAsync(i => i.MaterialId == id, cancellationToken);

        if (utilisee)
        {
            throw new RegleMetierException(
                $"La matière « {matiere.Name} » possède un historique. " +
                "Désactivez-la au lieu de la supprimer, afin de conserver les mouvements passés.");
        }

        _context.Materials.Remove(matiere);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Material), id.ToString(),
            $"Suppression de la matière « {matiere.Name} ».", null, cancellationToken);
    }

    public async Task<SyntheseStockDto> SyntheseAsync(CancellationToken cancellationToken = default)
    {
        var donnees = await _context.Materials
            .AsNoTracking()
            .Where(m => m.IsActive)
            .Select(m => new { m.CurrentQuantity, m.MinimumStock, m.AverageCost })
            .ToListAsync(cancellationToken);

        return new SyntheseStockDto(
            donnees.Count,
            donnees.Count(m => m.CurrentQuantity <= m.MinimumStock),
            Math.Round(donnees.Sum(m => m.CurrentQuantity * m.AverageCost), 2));
    }

    public async Task<IReadOnlyList<LotMatiereDto>> ListerLotsAsync(
        int matiereId, CancellationToken cancellationToken = default)
        => await _context.MaterialBatches
            .Include(l => l.Material)
            .AsNoTracking()
            .Where(l => l.MaterialId == matiereId)
            .OrderByDescending(l => l.ReceivedDate)
            .Select(l => new LotMatiereDto(
                l.Id, l.BatchNumber, l.MaterialId, l.Material.Name,
                l.Quantity, l.RemainingQuantity, l.UnitCost,
                l.ReceivedDate, l.ExpiryDate, l.Location, l.Notes))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<MatiereDto>> ListerStockFaibleAsync(
        CancellationToken cancellationToken = default)
        => await _context.Materials
            .Include(m => m.MaterialCategory).Include(m => m.Unit).Include(m => m.Supplier)
            .AsNoTracking()
            .Where(m => m.IsActive && m.CurrentQuantity <= m.MinimumStock)
            .OrderBy(m => m.Name)
            .Select(Projection)
            .ToListAsync(cancellationToken);

    private async Task VerifierReferencesAsync(MatiereRequete requete, CancellationToken cancellationToken)
    {
        if (!await _context.MaterialCategories.AnyAsync(c => c.Id == requete.CategorieId, cancellationToken))
        {
            throw new RegleMetierException("La catégorie sélectionnée n'existe pas.");
        }

        if (!await _context.Units.AnyAsync(u => u.Id == requete.UniteId, cancellationToken))
        {
            throw new RegleMetierException("L'unité de mesure sélectionnée n'existe pas.");
        }

        if (requete.StockMinimum < 0)
        {
            throw new RegleMetierException("Le stock minimum ne peut pas être négatif.");
        }

        if (requete.StockMaximum is not null && requete.StockMaximum < requete.StockMinimum)
        {
            throw new RegleMetierException("Le stock maximum doit être supérieur au stock minimum.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static readonly Expression<Func<Material, MatiereDto>> Projection = m => new MatiereDto(
        m.Id,
        m.Reference,
        m.Name,
        m.MaterialCategoryId,
        m.MaterialCategory.Name,
        m.UnitId,
        m.Unit.Code,
        m.CurrentQuantity,
        m.MinimumStock,
        m.MaximumStock,
        m.AverageCost,
        m.LastPurchasePrice,
        m.SupplierId,
        m.Supplier != null ? m.Supplier.Name : null,
        m.Location,
        m.Description,
        m.ImagePath,
        m.IsActive,
        m.CurrentQuantity * m.AverageCost,
        m.CurrentQuantity <= m.MinimumStock);
}
