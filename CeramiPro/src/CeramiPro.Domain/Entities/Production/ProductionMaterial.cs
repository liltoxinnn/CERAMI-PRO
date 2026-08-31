using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Materials;

namespace CeramiPro.Domain.Entities.Production;

/// <summary>Matière réservée puis consommée par un ordre de production.</summary>
public class ProductionMaterial : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal PlannedQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }
    public string? Notes { get; set; }
}
