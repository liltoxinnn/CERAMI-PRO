using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum NotificationSeverity
{
    [Libelle("Information")] Information = 0,
    [Libelle("Avertissement")] Avertissement = 1,
    [Libelle("Critique")] Critique = 2
}
