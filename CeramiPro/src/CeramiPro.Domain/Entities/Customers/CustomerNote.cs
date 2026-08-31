using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;

namespace CeramiPro.Domain.Entities.Customers;

public class CustomerNote : AuditableEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }
}
