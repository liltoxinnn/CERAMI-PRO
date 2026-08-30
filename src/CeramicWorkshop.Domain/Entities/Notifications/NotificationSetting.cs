using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Notifications;

/// <summary>Réglage d'une alerte : activation, seuil et rôle destinataire.</summary>
public class NotificationSetting : AuditableEntity
{
    public NotificationType Type { get; set; }
    public bool IsEnabled { get; set; } = true;

    /// <summary>Nombre de jours avant échéance déclenchant l'alerte.</summary>
    public int? ThresholdDays { get; set; }

    /// <summary>Seuil de valeur déclenchant l'alerte (montant ou quantité).</summary>
    public decimal? ThresholdValue { get; set; }

    public int? NotifyRoleId { get; set; }
    public Role? NotifyRole { get; set; }
}
