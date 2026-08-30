using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Quality;

/// <summary>Défaut relevé lors d'un contrôle qualité.</summary>
public class QualityIssue : BaseEntity
{
    public int QualityCheckId { get; set; }
    public QualityCheck QualityCheck { get; set; } = null!;

    public QualityCheckPoint CheckPoint { get; set; }
    public IssueSeverity Severity { get; set; }
    public IssueResolution Resolution { get; set; } = IssueResolution.ADecider;

    public decimal Quantity { get; set; }
    public string Description { get; set; } = null!;
    public string? Solution { get; set; }
}
