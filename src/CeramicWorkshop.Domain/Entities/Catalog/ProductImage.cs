using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.Catalog;

public class ProductImage : AuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string FilePath { get; set; } = null!;
    public string? Caption { get; set; }
    public ProductImageKind Kind { get; set; } = ProductImageKind.Supplementaire;
    public bool IsPrimary { get; set; }
    public int SortOrder { get; set; }
}
