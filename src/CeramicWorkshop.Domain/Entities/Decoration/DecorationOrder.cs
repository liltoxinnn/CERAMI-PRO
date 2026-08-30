using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.CustomOrders;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Entities.Production;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Decoration;

/// <summary>Travail de décoration réalisé sur une série de pièces.</summary>
public class DecorationOrder : AuditableEntity
{
    public string Reference { get; set; } = null!;

    public int DecorationTypeId { get; set; }
    public DecorationType DecorationType { get; set; } = null!;

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int? CustomOrderId { get; set; }
    public CustomOrder? CustomOrder { get; set; }

    public decimal Quantity { get; set; }
    public DecorationStatus Status { get; set; } = DecorationStatus.Planifiee;

    public string? Colors { get; set; }
    public string? Glaze { get; set; }
    public string? Paint { get; set; }

    /// <summary>Quantité d'or décoratif utilisée (en grammes).</summary>
    public decimal? GoldQuantity { get; set; }

    /// <summary>Quantité d'argent décoratif utilisée (en grammes).</summary>
    public decimal? SilverQuantity { get; set; }

    public string? MaterialsUsed { get; set; }
    public decimal Cost { get; set; }

    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public string? Notes { get; set; }

    public ICollection<DecorationImage> Images { get; set; } = new List<DecorationImage>();
}
