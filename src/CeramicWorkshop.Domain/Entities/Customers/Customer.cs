using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.CustomOrders;
using CeramicWorkshop.Domain.Entities.Invoicing;
using CeramicWorkshop.Domain.Entities.Payments;
using CeramicWorkshop.Domain.Entities.Sales;

namespace CeramicWorkshop.Domain.Entities.Customers;

public class Customer : AuditableEntity
{
    public string CustomerNumber { get; set; } = null!;
    public string FullName { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Notes { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<CustomerNote> CustomerNotes { get; set; } = new List<CustomerNote>();
    public ICollection<CustomOrder> CustomOrders { get; set; } = new List<CustomOrder>();
    public ICollection<Sale> Sales { get; set; } = new List<Sale>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
