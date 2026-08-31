namespace CeramiPro.Domain.Entities.Identity;

/// <summary>Association entre un rôle et une permission (clé composite).</summary>
public class RolePermission
{
    public int RoleId { get; set; }
    public Role Role { get; set; } = null!;

    public int PermissionId { get; set; }
    public Permission Permission { get; set; } = null!;
}
