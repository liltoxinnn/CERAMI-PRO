using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Catalog;

/// <summary>Catégorie de produit : vases, statues, assiettes, décorations murales…</summary>
public class ProductCategory : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
