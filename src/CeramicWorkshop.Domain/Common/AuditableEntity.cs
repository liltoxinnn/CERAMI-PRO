namespace CeramicWorkshop.Domain.Common;

/// <summary>
/// Entité traçable : date de création / modification et utilisateur responsable.
/// Ces champs sont remplis automatiquement par le contexte de données.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}
