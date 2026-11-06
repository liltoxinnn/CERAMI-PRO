using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Catalog;

/// <summary>
/// Photographie d'un produit. Une pièce faite à la main se vend d'abord par
/// son image : la fiche en accepte plusieurs, dont une principale.
/// </summary>
public class ProductImage : AuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string FilePath { get; set; } = null!;
    public string? Caption { get; set; }
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
