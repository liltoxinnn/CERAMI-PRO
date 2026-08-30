using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Production;

/// <summary>Historique daté de chaque étape de fabrication (tableau de production).</summary>
public class ProductionStageHistory : BaseEntity
{
    public int ProductionOrderId { get; set; }
    public ProductionOrder ProductionOrder { get; set; } = null!;

    public ProductionStatus Stage { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public decimal AcceptedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }
    public string? Notes { get; set; }
}
