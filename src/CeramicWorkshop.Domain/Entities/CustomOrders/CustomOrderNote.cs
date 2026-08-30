using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;

namespace CeramicWorkshop.Domain.Entities.CustomOrders;

public class CustomOrderNote : AuditableEntity
{
    public int CustomOrderId { get; set; }
    public CustomOrder CustomOrder { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }
}
