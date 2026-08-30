using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Entities.Catalog;

/// <summary>Déclinaison d'un produit (taille, couleur, finition).</summary>
public class ProductVariant : AuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Color { get; set; }
    public string? Size { get; set; }

    /// <summary>Écart de prix appliqué au prix de vente du produit de base.</summary>
    public decimal PriceAdjustment { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }
    public string? Barcode { get; set; }
    public bool IsActive { get; set; } = true;
}
