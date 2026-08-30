using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;

namespace CeramicWorkshop.Domain.Entities.Customers;

public class CustomerNote : AuditableEntity
{
    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Content { get; set; } = null!;

    public int? UserId { get; set; }
    public User? User { get; set; }
}
