using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Entities.Identity;

/// <summary>Droit unitaire (ex. « ventes.creer ») rattaché à un module fonctionnel.</summary>
public class Permission : BaseEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string Module { get; set; } = null!;
    public string? Description { get; set; }

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
