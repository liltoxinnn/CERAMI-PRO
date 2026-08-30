using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Catalog;

namespace CeramicWorkshop.Domain.Entities.Invoicing;

public class InvoiceItem : BaseEntity
{
    public int InvoiceId { get; set; }
    public Invoice Invoice { get; set; } = null!;

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
}
