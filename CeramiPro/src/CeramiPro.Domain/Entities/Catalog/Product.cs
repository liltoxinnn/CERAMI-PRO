using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Production;
using CeramiPro.Domain.Entities.Recipes;
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

    /// <summary>Coût de production de référence, mis à jour depuis les productions réelles.</summary>
    public decimal ProductionCost { get; set; }
    public decimal SellingPrice { get; set; }

    public decimal CurrentStock { get; set; }
    public decimal MinimumStock { get; set; }

    public string? Barcode { get; set; }
    public string? QrCode { get; set; }

    public bool IsCustomizable { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<ProductVariant> Variants { get; set; } = new List<ProductVariant>();
    public ICollection<ProductRecipe> Recipes { get; set; } = new List<ProductRecipe>();
    public ICollection<ProductionOrder> ProductionOrders { get; set; } = new List<ProductionOrder>();
    public ICollection<SaleItem> SaleItems { get; set; } = new List<SaleItem>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
