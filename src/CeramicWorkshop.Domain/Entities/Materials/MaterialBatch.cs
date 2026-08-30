using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Purchasing;

namespace CeramicWorkshop.Domain.Entities.Materials;

/// <summary>Lot de matière reçu : permet de suivre le coût réel et la traçabilité.</summary>
public class MaterialBatch : AuditableEntity
{
    public string BatchNumber { get; set; } = null!;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public int? PurchaseItemId { get; set; }
    public PurchaseItem? PurchaseItem { get; set; }

    public decimal Quantity { get; set; }
    public decimal RemainingQuantity { get; set; }
    public decimal UnitCost { get; set; }

    public DateTime ReceivedDate { get; set; }
    public DateTime? ExpiryDate { get; set; }
    public string? Location { get; set; }
    public string? Notes { get; set; }
}
