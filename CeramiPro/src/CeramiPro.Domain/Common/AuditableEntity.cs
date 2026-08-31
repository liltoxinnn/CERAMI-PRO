namespace CeramiPro.Domain.Common;

/// <summary>
/// Entité traçable : date de création, de modification et utilisateur
/// responsable. Ces champs sont remplis automatiquement par le contexte de
/// données, jamais à la main.
/// </summary>
public abstract class AuditableEntity : BaseEntity
{
    public DateTime CreatedAt { get; set; }
    public int? CreatedByUserId { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public int? UpdatedByUserId { get; set; }
}
