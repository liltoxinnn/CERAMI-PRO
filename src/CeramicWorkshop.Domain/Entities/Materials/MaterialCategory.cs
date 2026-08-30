using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Entities.Materials;

/// <summary>Catégorie de matière première : argile, émaux, pigments, emballage…</summary>
public class MaterialCategory : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Material> Materials { get; set; } = new List<Material>();
}
