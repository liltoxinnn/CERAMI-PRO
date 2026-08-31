using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Audit;

/// <summary>
/// Journal des opérations importantes (règle métier n°20) :
/// qui a créé une vente, modifié un stock, enregistré un paiement, annulé une transaction.
/// </summary>
public class AuditLog : BaseEntity
{
    public int? UserId { get; set; }
    public User? User { get; set; }

    /// <summary>Nom d'utilisateur figé au moment de l'action.</summary>
    public string? UserName { get; set; }

    public AuditAction Action { get; set; }
    public string EntityName { get; set; } = null!;
    public string? EntityId { get; set; }

    /// <summary>Détail des valeurs modifiées, au format JSON.</summary>
    public string? Changes { get; set; }

    public string? Description { get; set; }
    public string? IpAddress { get; set; }
    public DateTime OccurredAt { get; set; }
}
