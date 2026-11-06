using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Sales;

namespace CeramiPro.Domain.Entities.Catalog;

/// <summary>Produit céramique du catalogue de l'atelier.</summary>
public class Product : AuditableEntity
{
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;

    public int ProductCategoryId { get; set; }
    public ProductCategory ProductCategory { get; set; } = null!;

    public string? Description { get; set; }

    /// <summary>Matière principale décrite en clair (grès, faïence, porcelaine…).</summary>
    public string? MaterialDescription { get; set; }
    public string? Color { get; set; }
    public string? Finish { get; set; }

    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public decimal? Weight { get; set; }

    /// <summary>Coût de revient d'une pièce, en moyenne pondérée des entrées en stock.</summary>
    public decimal ProductionCost { get; set; }
    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public bool IsCustomizable { get; set; }

    /// <summary>
    /// Pièce faite à la main en un seul exemplaire : son stock vaut 0 ou 1,
    /// et une fois vendue elle ne déclenche aucune alerte de réapprovisionnement.
    /// </summary>
    public bool IsUniquePiece { get; set; }

    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Jeton de concurrence (colonne système « xmin » de PostgreSQL) : deux
    /// postes qui modifient le même stock en même temps ne s'écrasent pas.
    /// </summary>
    public uint Version { get; set; }

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
