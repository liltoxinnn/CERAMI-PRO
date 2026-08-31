using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Firing;

/// <summary>Lot de cuisson (une fournée) avec sa température, sa durée et son coût énergétique.</summary>
public class FiringBatch : AuditableEntity
{
    public string BatchNumber { get; set; } = null!;

    public int KilnId { get; set; }
    public Kiln Kiln { get; set; } = null!;

    public FiringType FiringType { get; set; }
    public FiringBatchStatus Status { get; set; } = FiringBatchStatus.Planifiee;

    public decimal Temperature { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime? EndTime { get; set; }

    /// <summary>Durée de cuisson en heures, calculée à la clôture du lot.</summary>
    public decimal? DurationHours => EndTime.HasValue
        ? Math.Round((decimal)(EndTime.Value - StartTime).TotalHours, 2)
        : null;

    public decimal EnergyCost { get; set; }
    public decimal DamagedQuantity { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public string? Observations { get; set; }

    public ICollection<FiringBatchItem> Items { get; set; } = new List<FiringBatchItem>();
}
