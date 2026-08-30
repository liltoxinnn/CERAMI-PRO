using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Catalogue;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Entities.Recipes;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Recettes de fabrication : quelles matières et quelles quantités pour un
/// nombre de pièces donné. Le calcul des besoins tient compte du pourcentage
/// de perte et compare avec le stock réellement disponible.
/// </summary>
public class RecetteService : IRecetteService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public RecetteService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<IReadOnlyList<RecetteDto>> ListerAsync(
        int? produitId = null, CancellationToken cancellationToken = default)
    {
        var requete = ChargerAvecDetails().AsNoTracking();

        if (produitId is not null)
        {
            requete = requete.Where(r => r.ProductId == produitId);
        }

        var recettes = await requete
            .OrderBy(r => r.Product.Name).ThenByDescending(r => r.IsDefault).ThenBy(r => r.Version)
            .ToListAsync(cancellationToken);

        return recettes.Select(Convertir).ToList();
    }

    public async Task<RecetteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var recette = await ChargerAvecDetails().AsNoTracking()
                          .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                      ?? throw NotFoundException.Pour("Recette", id);

        return Convertir(recette);
    }

    public async Task<RecetteDto> CreerAsync(
        RecetteRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierAsync(requete, cancellationToken);

        var version = await _context.ProductRecipes
            .Where(r => r.ProductId == requete.ProduitId)
            .Select(r => (int?)r.Version).MaxAsync(cancellationToken) ?? 0;

        var premiere = version == 0;

        var recette = new ProductRecipe
        {
            ProductId = requete.ProduitId,
            Name = requete.Nom.Trim(),
            Version = version + 1,
            YieldQuantity = requete.Rendement,
            LaborCost = requete.CoutMainOeuvre,
            FiringCost = requete.CoutCuisson,
            DecorationCost = requete.CoutDecoration,
            PackagingCost = requete.CoutEmballage,
            OtherCost = requete.AutresCouts,
            // La première recette d'un produit devient sa recette de référence.
            IsDefault = requete.ParDefaut || premiere,
            IsActive = requete.Active,
            Notes = Nettoyer(requete.Notes)
        };

        RemplirLignes(recette, requete);

        _context.ProductRecipes.Add(recette);
        await _context.SaveChangesAsync(cancellationToken);

        if (recette.IsDefault)
        {
            await DefinirRecetteParDefautAsync(recette, cancellationToken);
        }

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(ProductRecipe), recette.Id.ToString(),
            $"Création de la recette « {recette.Name} » (version {recette.Version}).", null, cancellationToken);

        return await ObtenirAsync(recette.Id, cancellationToken);
    }

    public async Task<RecetteDto> ModifierAsync(
        int id, RecetteRequete requete, CancellationToken cancellationToken = default)
    {
        var recette = await ChargerAvecDetails().FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                      ?? throw NotFoundException.Pour("Recette", id);

        await VerifierAsync(requete, cancellationToken);

        _context.ProductRecipeItems.RemoveRange(recette.Items);
        recette.Items.Clear();

        recette.Name = requete.Nom.Trim();
        recette.YieldQuantity = requete.Rendement;
        recette.LaborCost = requete.CoutMainOeuvre;
        recette.FiringCost = requete.CoutCuisson;
        recette.DecorationCost = requete.CoutDecoration;
        recette.PackagingCost = requete.CoutEmballage;
        recette.OtherCost = requete.AutresCouts;
        recette.IsDefault = requete.ParDefaut;
        recette.IsActive = requete.Active;
        recette.Notes = Nettoyer(requete.Notes);

        RemplirLignes(recette, requete);
        await _context.SaveChangesAsync(cancellationToken);

        if (recette.IsDefault)
        {
            await DefinirRecetteParDefautAsync(recette, cancellationToken);
        }

        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(ProductRecipe), id.ToString(),
            $"Modification de la recette « {recette.Name} ».", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var recette = await _context.ProductRecipes.FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                      ?? throw NotFoundException.Pour("Recette", id);

        var utilisee = await _context.ProductionOrders.IgnoreQueryFilters()
            .AnyAsync(o => o.ProductRecipeId == id, cancellationToken);

        if (utilisee)
        {
            throw new BusinessRuleException(
                $"La recette « {recette.Name} » a déjà servi à une production. " +
                "Désactivez-la au lieu de la supprimer.");
        }

        _context.ProductRecipes.Remove(recette);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(ProductRecipe), id.ToString(),
            $"Suppression de la recette « {recette.Name} ».", null, cancellationToken);
    }

    public async Task<BesoinsRecetteDto> CalculerBesoinsAsync(
        int recetteId, decimal quantite, CancellationToken cancellationToken = default)
    {
        if (quantite <= 0)
        {
            throw new BusinessRuleException("Indiquez le nombre de pièces à produire.");
        }

        var recette = await ChargerAvecDetails().AsNoTracking()
                          .FirstOrDefaultAsync(r => r.Id == recetteId, cancellationToken)
                      ?? throw NotFoundException.Pour("Recette", recetteId);

        var rendement = recette.YieldQuantity <= 0 ? 1m : recette.YieldQuantity;
        var facteur = quantite / rendement;

        var besoins = new List<BesoinMatiereDto>();

        foreach (var ligne in recette.Items)
        {
            var necessaire = Math.Round(
                ligne.Quantity * (1 + ligne.WastePercentage / 100m) * facteur, 4);
            var disponible = ligne.Material.CurrentQuantity;
            var manquant = Math.Max(0m, necessaire - disponible);

            besoins.Add(new BesoinMatiereDto(
                ligne.MaterialId,
                ligne.Material.Name,
                ligne.Unit.Code,
                necessaire,
                disponible,
                manquant,
                ligne.Material.AverageCost,
                Math.Round(necessaire * ligne.Material.AverageCost, 2),
                manquant == 0m));
        }

        var coutMatieres = Math.Round(besoins.Sum(b => b.Cout), 2);
        var coutAnnexes = Math.Round(
            (recette.LaborCost + recette.FiringCost + recette.DecorationCost
             + recette.PackagingCost + recette.OtherCost) * facteur, 2);
        var coutTotal = coutMatieres + coutAnnexes;

        return new BesoinsRecetteDto(
            recette.Id,
            recette.Name,
            recette.Product.Name,
            quantite,
            coutMatieres,
            coutTotal,
            quantite > 0 ? Math.Round(coutTotal / quantite, 2) : 0m,
            besoins.All(b => b.Suffisant),
            besoins);
    }

    private IQueryable<ProductRecipe> ChargerAvecDetails()
        => _context.ProductRecipes
            .Include(r => r.Product)
            .Include(r => r.Items).ThenInclude(i => i.Material)
            .Include(r => r.Items).ThenInclude(i => i.Unit);

    /// <summary>Une seule recette par produit peut être marquée comme référence.</summary>
    private async Task DefinirRecetteParDefautAsync(ProductRecipe recette, CancellationToken cancellationToken)
    {
        var autres = await _context.ProductRecipes
            .Where(r => r.ProductId == recette.ProductId && r.Id != recette.Id && r.IsDefault)
            .ToListAsync(cancellationToken);

        foreach (var autre in autres)
        {
            autre.IsDefault = false;
        }

        if (autres.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    private static void RemplirLignes(ProductRecipe recette, RecetteRequete requete)
    {
        foreach (var ligne in requete.Lignes)
        {
            recette.Items.Add(new ProductRecipeItem
            {
                MaterialId = ligne.MatiereId,
                UnitId = ligne.UniteId,
                Quantity = ligne.Quantite,
                WastePercentage = ligne.PourcentagePerte,
                Notes = Nettoyer(ligne.Notes)
            });
        }
    }

    private async Task VerifierAsync(RecetteRequete requete, CancellationToken cancellationToken)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == requete.ProduitId, cancellationToken))
        {
            throw new BusinessRuleException("Le produit sélectionné n'existe pas.");
        }

        if (requete.Rendement <= 0)
        {
            throw new BusinessRuleException("Le nombre de pièces obtenues doit être supérieur à zéro.");
        }

        if (requete.Lignes.Count == 0)
        {
            throw new BusinessRuleException("Ajoutez au moins une matière à la recette.");
        }

        if (requete.Lignes.Select(l => l.MatiereId).Distinct().Count() != requete.Lignes.Count)
        {
            throw new BusinessRuleException("Une même matière ne peut apparaître qu'une seule fois dans la recette.");
        }

        foreach (var ligne in requete.Lignes)
        {
            if (ligne.Quantite <= 0)
            {
                throw new BusinessRuleException("Chaque matière doit avoir une quantité supérieure à zéro.");
            }

            if (ligne.PourcentagePerte is < 0 or > 100)
            {
                throw new BusinessRuleException("Le pourcentage de perte doit être compris entre 0 et 100 %.");
            }

            if (!await _context.Materials.AnyAsync(m => m.Id == ligne.MatiereId, cancellationToken))
            {
                throw new BusinessRuleException("Une des matières sélectionnées n'existe pas.");
            }

            if (!await _context.Units.AnyAsync(u => u.Id == ligne.UniteId, cancellationToken))
            {
                throw new BusinessRuleException("Une des unités sélectionnées n'existe pas.");
            }
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static RecetteDto Convertir(ProductRecipe r)
    {
        var lignes = r.Items.Select(i =>
        {
            var avecPerte = Math.Round(i.Quantity * (1 + i.WastePercentage / 100m), 4);
            return new LigneRecetteDto(
                i.Id, i.MaterialId, i.Material.Name, i.Material.Reference,
                i.UnitId, i.Unit.Code, i.Quantity, i.WastePercentage, avecPerte,
                i.Material.AverageCost, Math.Round(avecPerte * i.Material.AverageCost, 2), i.Notes);
        }).ToList();

        var coutMatieres = Math.Round(lignes.Sum(l => l.Cout), 2);
        var coutTotal = Math.Round(
            coutMatieres + r.LaborCost + r.FiringCost + r.DecorationCost + r.PackagingCost + r.OtherCost, 2);
        var rendement = r.YieldQuantity <= 0 ? 1m : r.YieldQuantity;

        return new RecetteDto(
            r.Id, r.ProductId, r.Product.Name, r.Name, r.Version, r.YieldQuantity,
            r.LaborCost, r.FiringCost, r.DecorationCost, r.PackagingCost, r.OtherCost,
            coutMatieres, coutTotal, Math.Round(coutTotal / rendement, 2),
            r.IsDefault, r.IsActive, r.Notes, lignes);
    }
}
