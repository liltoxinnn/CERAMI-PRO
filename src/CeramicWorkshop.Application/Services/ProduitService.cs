using System.Linq.Expressions;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Catalogue;
using CeramicWorkshop.Application.DTOs.Stock;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Catalog;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Catalogue des produits céramiques : fiche, photos et variantes.
/// Le stock des produits finis évolue uniquement par mouvement d'inventaire.
/// </summary>
public class ProduitService : IProduitService
{
    private readonly IApplicationDbContext _context;
    private readonly IInventaireService _inventaire;
    private readonly IReferenceNumberService _numerotation;
    private readonly IAuditService _audit;

    public ProduitService(
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

    public async Task<PagedResult<ProduitDto>> ListerAsync(
        FiltreProduitsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.Products
            .Include(p => p.ProductCategory)
            .Include(p => p.Images)
            .AsNoTracking()
            .AsQueryable();

        if (!requete.InclureInactifs)
        {
            requeteBase = requeteBase.Where(p => p.IsActive);
        }

        if (requete.CategorieId is not null)
        {
            requeteBase = requeteBase.Where(p => p.ProductCategoryId == requete.CategorieId);
        }

        if (requete.SeulementStockFaible)
        {
            requeteBase = requeteBase.Where(p => p.CurrentStock <= p.MinimumStock);
        }

        if (requete.SeulementPersonnalisables)
        {
            requeteBase = requeteBase.Where(p => p.IsCustomizable);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(p =>
                p.Name.ToLower().Contains(recherche) ||
                p.Reference.ToLower().Contains(recherche) ||
                (p.Barcode != null && p.Barcode.ToLower().Contains(recherche)) ||
                (p.Color != null && p.Color.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var elements = await requeteBase
            .OrderBy(p => p.Name)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .Select(Projection)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProduitDto>(elements, total, requete.Page, requete.TaillePage);
    }

    public async Task<ProduitDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Products
               .Include(p => p.ProductCategory).Include(p => p.Images)
               .AsNoTracking().Where(p => p.Id == id).Select(Projection)
               .FirstOrDefaultAsync(cancellationToken)
           ?? throw NotFoundException.Pour("Produit", id);

    public async Task<ProduitDto?> RechercherParCodeAsync(
        string code, CancellationToken cancellationToken = default)
    {
        var recherche = (code ?? string.Empty).Trim().ToLower();

        if (recherche.Length == 0)
        {
            return null;
        }

        return await _context.Products
            .Include(p => p.ProductCategory).Include(p => p.Images)
            .AsNoTracking()
            .Where(p => p.Barcode!.ToLower() == recherche
                        || p.Reference.ToLower() == recherche
                        || p.QrCode!.ToLower() == recherche)
            .Select(Projection)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<ProduitDto> CreerAsync(
        ProduitRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierAsync(requete, null, cancellationToken);

        var reference = await _numerotation.GenererAsync(TypeDocument.Produit, cancellationToken);

        var produit = new Product
        {
            Reference = reference,
            Name = requete.Nom.Trim(),
            ProductCategoryId = requete.CategorieId,
            Description = Nettoyer(requete.Description),
            MaterialDescription = Nettoyer(requete.Matiere),
            Color = Nettoyer(requete.Couleur),
            Finish = Nettoyer(requete.Finition),
            Width = requete.Largeur,
            Height = requete.Hauteur,
            Depth = requete.Profondeur,
            Weight = requete.Poids,
            ProductionCost = requete.CoutProduction,
            SellingPrice = requete.PrixVente,
            MinimumStock = requete.StockMinimum,
            Barcode = Nettoyer(requete.CodeBarres) ?? reference,
            QrCode = reference,
            IsCustomizable = requete.Personnalisable,
            IsActive = requete.Actif
        };

        _context.Products.Add(produit);
        await _context.SaveChangesAsync(cancellationToken);

        if (requete.StockInitial > 0)
        {
            await _inventaire.EnregistrerAsync(new MouvementStockRequete
            {
                TypeArticle = InventoryItemType.ProduitFini,
                TypeMouvement = InventoryTransactionType.Ajustement,
                ProduitId = produit.Id,
                Quantite = requete.StockInitial,
                CoutUnitaire = requete.CoutProduction,
                Reference = produit.Reference,
                Notes = "Pièces présentes dans l'atelier à la création de la fiche."
            }, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Product), produit.Id.ToString(),
            $"Création du produit « {produit.Name} » ({produit.Reference}).", null, cancellationToken);

        return await ObtenirAsync(produit.Id, cancellationToken);
    }

    public async Task<ProduitDto> ModifierAsync(
        int id, ProduitRequete requete, CancellationToken cancellationToken = default)
    {
        var produit = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                      ?? throw NotFoundException.Pour("Produit", id);

        await VerifierAsync(requete, id, cancellationToken);

        produit.Name = requete.Nom.Trim();
        produit.ProductCategoryId = requete.CategorieId;
        produit.Description = Nettoyer(requete.Description);
        produit.MaterialDescription = Nettoyer(requete.Matiere);
        produit.Color = Nettoyer(requete.Couleur);
        produit.Finish = Nettoyer(requete.Finition);
        produit.Width = requete.Largeur;
        produit.Height = requete.Hauteur;
        produit.Depth = requete.Profondeur;
        produit.Weight = requete.Poids;
        produit.ProductionCost = requete.CoutProduction;
        produit.SellingPrice = requete.PrixVente;
        produit.MinimumStock = requete.StockMinimum;
        produit.Barcode = Nettoyer(requete.CodeBarres) ?? produit.Reference;
        produit.IsCustomizable = requete.Personnalisable;
        produit.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Product), id.ToString(),
            $"Modification du produit « {produit.Name} ».", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var produit = await _context.Products.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                      ?? throw NotFoundException.Pour("Produit", id);

        var utilise = await _context.InventoryTransactions.AnyAsync(t => t.ProductId == id, cancellationToken)
                      || await _context.SaleItems.IgnoreQueryFilters().AnyAsync(i => i.ProductId == id, cancellationToken)
                      || await _context.ProductionOrders.IgnoreQueryFilters()
                          .AnyAsync(o => o.ProductId == id, cancellationToken);

        if (utilise)
        {
            throw new BusinessRuleException(
                $"Le produit « {produit.Name} » possède un historique. " +
                "Désactivez-le au lieu de le supprimer.");
        }

        _context.Products.Remove(produit);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Product), id.ToString(),
            $"Suppression du produit « {produit.Name} ».", null, cancellationToken);
    }

    public async Task<SyntheseCatalogueDto> SyntheseAsync(CancellationToken cancellationToken = default)
    {
        var donnees = await _context.Products
            .AsNoTracking().Where(p => p.IsActive)
            .Select(p => new { p.CurrentStock, p.MinimumStock, p.ProductionCost, p.SellingPrice })
            .ToListAsync(cancellationToken);

        var margesConnues = donnees.Where(p => p.SellingPrice > 0).ToList();

        return new SyntheseCatalogueDto(
            donnees.Count,
            donnees.Count(p => p.CurrentStock <= p.MinimumStock),
            Math.Round(donnees.Sum(p => p.CurrentStock * p.ProductionCost), 2),
            margesConnues.Count == 0
                ? 0m
                : Math.Round(margesConnues.Average(p => (p.SellingPrice - p.ProductionCost) / p.SellingPrice * 100m), 1));
    }

    public async Task<IReadOnlyList<ProduitDto>> ListerStockFaibleAsync(
        CancellationToken cancellationToken = default)
        => await _context.Products
            .Include(p => p.ProductCategory).Include(p => p.Images)
            .AsNoTracking()
            .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStock)
            .OrderBy(p => p.Name)
            .Select(Projection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<PhotoProduitDto>> ListerPhotosAsync(
        int produitId, CancellationToken cancellationToken = default)
    {
        var photos = await _context.ProductImages
            .AsNoTracking()
            .Where(i => i.ProductId == produitId)
            .OrderByDescending(i => i.IsPrimary).ThenBy(i => i.SortOrder).ThenBy(i => i.Id)
            .ToListAsync(cancellationToken);

        return photos
            .Select(i => new PhotoProduitDto(i.Id, i.ProductId, i.FilePath, i.Caption,
                i.Kind, i.Kind.Libelle(), i.IsPrimary, i.SortOrder))
            .ToList();
    }

    public async Task<PhotoProduitDto> AjouterPhotoAsync(
        int produitId, PhotoProduitRequete requete, CancellationToken cancellationToken = default)
    {
        if (!await _context.Products.AnyAsync(p => p.Id == produitId, cancellationToken))
        {
            throw NotFoundException.Pour("Produit", produitId);
        }

        if (string.IsNullOrWhiteSpace(requete.Chemin))
        {
            throw new BusinessRuleException("Sélectionnez une photo à ajouter.");
        }

        if (requete.Principale)
        {
            foreach (var autre in await _context.ProductImages
                         .Where(i => i.ProductId == produitId && i.IsPrimary).ToListAsync(cancellationToken))
            {
                autre.IsPrimary = false;
            }
        }

        var ordre = await _context.ProductImages
            .Where(i => i.ProductId == produitId)
            .Select(i => (int?)i.SortOrder).MaxAsync(cancellationToken) ?? 0;

        var photo = new ProductImage
        {
            ProductId = produitId,
            FilePath = requete.Chemin.Trim(),
            Caption = Nettoyer(requete.Legende),
            Kind = requete.Type,
            IsPrimary = requete.Principale,
            SortOrder = ordre + 1
        };

        _context.ProductImages.Add(photo);
        await _context.SaveChangesAsync(cancellationToken);

        return new PhotoProduitDto(photo.Id, produitId, photo.FilePath, photo.Caption,
            photo.Kind, photo.Kind.Libelle(), photo.IsPrimary, photo.SortOrder);
    }

    public async Task SupprimerPhotoAsync(
        int produitId, int photoId, CancellationToken cancellationToken = default)
    {
        var photo = await _context.ProductImages
                        .FirstOrDefaultAsync(i => i.Id == photoId && i.ProductId == produitId, cancellationToken)
                    ?? throw NotFoundException.Pour("Photo", photoId);

        _context.ProductImages.Remove(photo);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<VarianteProduitDto>> ListerVariantesAsync(
        int produitId, CancellationToken cancellationToken = default)
        => await _context.ProductVariants
            .Include(v => v.Product)
            .AsNoTracking()
            .Where(v => v.ProductId == produitId)
            .OrderBy(v => v.Name)
            .Select(v => new VarianteProduitDto(
                v.Id, v.ProductId, v.Reference, v.Name, v.Color, v.Size,
                v.PriceAdjustment, v.Product.SellingPrice + v.PriceAdjustment,
                v.CurrentStock, v.MinimumStock, v.Barcode, v.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<VarianteProduitDto> AjouterVarianteAsync(
        int produitId, VarianteProduitRequete requete, CancellationToken cancellationToken = default)
    {
        var produit = await _context.Products.FirstOrDefaultAsync(p => p.Id == produitId, cancellationToken)
                      ?? throw NotFoundException.Pour("Produit", produitId);

        var nombre = await _context.ProductVariants.CountAsync(v => v.ProductId == produitId, cancellationToken);

        var variante = new ProductVariant
        {
            ProductId = produitId,
            Reference = $"{produit.Reference}-{nombre + 1:00}",
            Name = requete.Nom.Trim(),
            Color = Nettoyer(requete.Couleur),
            Size = Nettoyer(requete.Taille),
            PriceAdjustment = requete.AjustementPrix,
            MinimumStock = requete.StockMinimum,
            Barcode = Nettoyer(requete.CodeBarres),
            IsActive = requete.Actif
        };

        _context.ProductVariants.Add(variante);
        await _context.SaveChangesAsync(cancellationToken);

        return (await ListerVariantesAsync(produitId, cancellationToken)).First(v => v.Id == variante.Id);
    }

    public async Task<VarianteProduitDto> ModifierVarianteAsync(
        int produitId, int varianteId, VarianteProduitRequete requete,
        CancellationToken cancellationToken = default)
    {
        var variante = await _context.ProductVariants
                           .FirstOrDefaultAsync(v => v.Id == varianteId && v.ProductId == produitId, cancellationToken)
                       ?? throw NotFoundException.Pour("Variante", varianteId);

        variante.Name = requete.Nom.Trim();
        variante.Color = Nettoyer(requete.Couleur);
        variante.Size = Nettoyer(requete.Taille);
        variante.PriceAdjustment = requete.AjustementPrix;
        variante.MinimumStock = requete.StockMinimum;
        variante.Barcode = Nettoyer(requete.CodeBarres);
        variante.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);

        return (await ListerVariantesAsync(produitId, cancellationToken)).First(v => v.Id == varianteId);
    }

    public async Task SupprimerVarianteAsync(
        int produitId, int varianteId, CancellationToken cancellationToken = default)
    {
        var variante = await _context.ProductVariants
                           .FirstOrDefaultAsync(v => v.Id == varianteId && v.ProductId == produitId, cancellationToken)
                       ?? throw NotFoundException.Pour("Variante", varianteId);

        if (variante.CurrentStock != 0)
        {
            throw new BusinessRuleException(
                $"La variante « {variante.Name} » a encore du stock : désactivez-la au lieu de la supprimer.");
        }

        _context.ProductVariants.Remove(variante);
        await _context.SaveChangesAsync(cancellationToken);
    }

    private async Task VerifierAsync(ProduitRequete requete, int? idExclu, CancellationToken cancellationToken)
    {
        if (!await _context.ProductCategories.AnyAsync(c => c.Id == requete.CategorieId, cancellationToken))
        {
            throw new BusinessRuleException("La catégorie sélectionnée n'existe pas.");
        }

        if (requete.PrixVente < 0 || requete.CoutProduction < 0)
        {
            throw new BusinessRuleException("Le prix de vente et le coût de production ne peuvent pas être négatifs.");
        }

        var codeBarres = Nettoyer(requete.CodeBarres);

        if (codeBarres is not null && await _context.Products
                .AnyAsync(p => p.Id != idExclu && p.Barcode == codeBarres, cancellationToken))
        {
            throw new BusinessRuleException($"Le code-barres « {codeBarres} » est déjà utilisé par un autre produit.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static readonly Expression<Func<Product, ProduitDto>> Projection = p => new ProduitDto(
        p.Id,
        p.Reference,
        p.Name,
        p.ProductCategoryId,
        p.ProductCategory.Name,
        p.Description,
        p.MaterialDescription,
        p.Color,
        p.Finish,
        p.Width,
        p.Height,
        p.Depth,
        p.Weight,
        p.ProductionCost,
        p.SellingPrice,
        p.SellingPrice - p.ProductionCost,
        p.SellingPrice > 0 ? (p.SellingPrice - p.ProductionCost) / p.SellingPrice * 100m : 0m,
        p.CurrentStock,
        p.MinimumStock,
        p.Barcode,
        p.QrCode,
        p.IsCustomizable,
        p.IsActive,
        p.CurrentStock <= p.MinimumStock,
        p.Images.Where(i => i.IsPrimary).Select(i => i.FilePath).FirstOrDefault(),
        p.Images.Count,
        p.Variants.Count,
        p.Recipes.Count);
}
