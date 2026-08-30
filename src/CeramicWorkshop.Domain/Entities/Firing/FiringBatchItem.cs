using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Catalog;
using CeramicWorkshop.Domain.Entities.Production;

namespace CeramicWorkshop.Domain.Entities.Firing;

/// <summary>Pièces enfournées dans un lot de cuisson.</summary>
public class FiringBatchItem : BaseEntity
{
    public int FiringBatchId { get; set; }
    public FiringBatch FiringBatch { get; set; } = null!;

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public decimal Quantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }

    /// <summary>Part du coût énergétique du lot imputée à ces pièces.</summary>
    public decimal AllocatedEnergyCost { get; set; }

    public string? Notes { get; set; }
}
