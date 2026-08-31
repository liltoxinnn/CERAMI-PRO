using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;

namespace CeramiPro.Domain.Entities.CustomOrders;

public class CustomOrderNote : AuditableEntity
{
    public int CustomOrderId { get; set; }
    public CustomOrder CustomOrder { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }
}
