using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Materials;

namespace CeramicWorkshop.Domain.Entities.Purchasing;

public class PurchaseItem : BaseEntity
{
    public int PurchaseId { get; set; }
    public Purchase Purchase { get; set; } = null!;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal ReceivedQuantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public string? Notes { get; set; }

    public ICollection<MaterialBatch> Batches { get; set; } = new List<MaterialBatch>();
}
