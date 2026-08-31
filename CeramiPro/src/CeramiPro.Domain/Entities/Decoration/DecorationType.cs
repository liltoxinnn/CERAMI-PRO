using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Decoration;

/// <summary>Type de décoration : émaillage, peinture à la main, dorure, argenture…</summary>
public class DecorationType : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<DecorationOrder> DecorationOrders { get; set; } = new List<DecorationOrder>();
}
