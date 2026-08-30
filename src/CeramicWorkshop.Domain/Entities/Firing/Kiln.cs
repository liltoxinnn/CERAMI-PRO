using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Firing;

/// <summary>Four de l'atelier.</summary>
public class Kiln : AuditableEntity
{
    public string Reference { get; set; } = null!;
    public string Name { get; set; } = null!;

    /// <summary>Capacité exprimée en nombre de pièces.</summary>
    public decimal Capacity { get; set; }

    public decimal MinTemperature { get; set; }
    public decimal MaxTemperature { get; set; }

    public string? Location { get; set; }
    public KilnStatus Status { get; set; } = KilnStatus.Disponible;
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<FiringBatch> FiringBatches { get; set; } = new List<FiringBatch>();
}
