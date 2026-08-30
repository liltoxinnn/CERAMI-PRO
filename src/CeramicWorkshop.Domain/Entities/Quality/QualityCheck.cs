using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.CustomOrders;
using CeramicWorkshop.Domain.Entities.Firing;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Entities.Production;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Quality;

/// <summary>
/// Contrôle qualité obligatoire avant l'entrée en stock des produits finis
/// (règle métier n°10).
/// </summary>
public class QualityCheck : AuditableEntity
{
    public string Reference { get; set; } = null!;

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int? CustomOrderId { get; set; }
    public CustomOrder? CustomOrder { get; set; }

    public int? FiringBatchId { get; set; }
    public FiringBatch? FiringBatch { get; set; }

    public DateTime CheckedAt { get; set; }

    public int? CheckedByUserId { get; set; }
    public User? CheckedByUser { get; set; }

    public decimal InspectedQuantity { get; set; }
    public decimal AcceptedQuantity { get; set; }
    public decimal RejectedQuantity { get; set; }
    public decimal ReworkQuantity { get; set; }

    public QualityResult Result { get; set; }

    // Points de contrôle : vrai lorsque le point est conforme.
    public bool CracksOk { get; set; } = true;
    public bool ShapeOk { get; set; } = true;
    public bool ColorOk { get; set; } = true;
    public bool GlazeOk { get; set; } = true;
    public bool DecorationOk { get; set; } = true;
    public bool DimensionsOk { get; set; } = true;
    public bool SurfaceOk { get; set; } = true;
    public bool FiringOk { get; set; } = true;

    public string? Notes { get; set; }

    public ICollection<QualityIssue> Issues { get; set; } = new List<QualityIssue>();
}
