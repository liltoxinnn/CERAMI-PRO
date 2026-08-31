using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Identity;

/// <summary>Rôle applicatif : Administrateur, Responsable, Employé, Caissier.</summary>
public class Role : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? Description { get; set; }

    /// <summary>Rôle livré avec le logiciel : il ne peut pas être supprimé.</summary>
    public bool IsSystem { get; set; }

    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
