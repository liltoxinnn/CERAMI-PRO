using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Audit;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Identity;

/// <summary>Utilisateur du logiciel. Le mot de passe n'est jamais stocké en clair.</summary>
public class User : AuditableEntity
{
    public string UserName { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }

    /// <summary>Empreinte du mot de passe (algorithme PBKDF2 avec sel aléatoire).</summary>
    public string PasswordHash { get; set; } = null!;

    /// <summary>
    /// Rôle, porté par l'utilisateur lui-même : l'atelier n'a pas assez de
    /// monde pour qu'une table de rôles et une table de droits se justifient.
    /// </summary>
    public RoleUtilisateur Role { get; set; } = RoleUtilisateur.Employe;

    public bool IsActive { get; set; } = true;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockedUntil { get; set; }
    public bool MustChangePassword { get; set; }

    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
}
