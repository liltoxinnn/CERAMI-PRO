using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Production;
using CeramiPro.Domain.Entities.Purchasing;
using CeramiPro.Domain.Entities.Recipes;
using CeramiPro.Domain.Entities.Suppliers;

namespace CeramiPro.Domain.Entities.Materials;

/// <summary>
/// Matière première ou consommable de l'atelier.
/// La quantité en stock n'est modifiée que par un mouvement d'inventaire (règle métier n°2).
/// </summary>
public class Material : AuditableEntity
{
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;

    public int MaterialCategoryId { get; set; }
    public MaterialCategory MaterialCategory { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal CurrentQuantity { get; set; }
    public decimal MinimumStock { get; set; }
    public decimal? MaximumStock { get; set; }

    /// <summary>Coût moyen pondéré, recalculé à chaque réception.</summary>
    public decimal AverageCost { get; set; }
    public decimal LastPurchasePrice { get; set; }

    public int? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public string? Location { get; set; }
    public string? Description { get; set; }
    public string? ImagePath { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<MaterialBatch> Batches { get; set; } = new List<MaterialBatch>();
    public ICollection<PurchaseItem> PurchaseItems { get; set; } = new List<PurchaseItem>();
    public ICollection<ProductRecipeItem> RecipeItems { get; set; } = new List<ProductRecipeItem>();
    public ICollection<ProductionMaterial> ProductionMaterials { get; set; } = new List<ProductionMaterial>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
