using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Inventory;

/// <summary>Régularisation de stock justifiée (inventaire physique, casse, correction).</summary>
public class StockAdjustment : AuditableEntity
{
    public string Reference { get; set; } = null!;
    public InventoryItemType ItemType { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public StockAdjustmentReason Reason { get; set; }

    public decimal QuantityBefore { get; set; }
    public decimal CountedQuantity { get; set; }
    public decimal Difference { get; set; }

    public DateTime AdjustmentDate { get; set; }
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public ICollection<InventoryTransaction> Transactions { get; set; } = new List<InventoryTransaction>();
}
