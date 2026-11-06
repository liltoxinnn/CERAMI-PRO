using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Catalog;

/// <summary>Catégorie de produit : vases, statues, assiettes, décorations murales…</summary>
public class ProductCategory : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    /// <summary>Catégorie livrée avec le logiciel : elle se désactive, ne se supprime pas.</summary>
    public bool IsSystem { get; set; }

    public ICollection<Product> Products { get; set; } = new List<Product>();
}
