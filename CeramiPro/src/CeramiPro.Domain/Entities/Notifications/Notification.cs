using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Notifications;

public class Notification : AuditableEntity
{
    public NotificationType Type { get; set; }
    public NotificationSeverity Severity { get; set; } = NotificationSeverity.Information;

    public string Title { get; set; } = null!;
    public string Message { get; set; } = null!;

    /// <summary>Lien interne vers la fiche concernée (ex. /production/12).</summary>
    public string? Link { get; set; }

    public string? EntityName { get; set; }
    public int? EntityId { get; set; }

    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }

    /// <summary>Destinataire ; vide = visible par tous les utilisateurs habilités.</summary>
    public int? UserId { get; set; }
    public User? User { get; set; }
}
