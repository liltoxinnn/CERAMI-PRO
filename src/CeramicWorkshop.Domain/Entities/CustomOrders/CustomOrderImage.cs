using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Domain.Entities.CustomOrders;

/// <summary>Photo de référence, croquis ou photo de fabrication d'une commande personnalisée.</summary>
public class CustomOrderImage : AuditableEntity
{
    public int CustomOrderId { get; set; }
    public CustomOrder CustomOrder { get; set; } = null!;

    public string FilePath { get; set; } = null!;
    public string? Caption { get; set; }
    public CustomOrderImageKind Kind { get; set; } = CustomOrderImageKind.Reference;
    public int SortOrder { get; set; }
}
