using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Decoration;

public class DecorationImage : AuditableEntity
{
    public int DecorationOrderId { get; set; }
    public DecorationOrder DecorationOrder { get; set; } = null!;

    public string FilePath { get; set; } = null!;
    public string? Caption { get; set; }
    public int SortOrder { get; set; }
}
