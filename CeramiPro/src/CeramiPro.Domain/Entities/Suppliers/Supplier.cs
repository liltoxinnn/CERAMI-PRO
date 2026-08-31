using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Entities.Purchasing;

namespace CeramiPro.Domain.Entities.Suppliers;

public class Supplier : AuditableEntity
{
    public string SupplierNumber { get; set; } = null!;
    public string Name { get; set; } = null!;
    public string? CompanyName { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Material> Materials { get; set; } = new List<Material>();
    public ICollection<Purchase> Purchases { get; set; } = new List<Purchase>();
    public ICollection<SupplierPayment> SupplierPayments { get; set; } = new List<SupplierPayment>();
}
