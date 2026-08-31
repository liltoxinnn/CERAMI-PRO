using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Entities.Sales;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Invoicing;

/// <summary>Facture client émise pour une vente ou une commande personnalisée.</summary>
public class Invoice : AuditableEntity, ISoftDeletable
{
    public string InvoiceNumber { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int? CustomOrderId { get; set; }
    public CustomOrder? CustomOrder { get; set; }

    public DateTime IssueDate { get; set; }
    public DateTime? DueDate { get; set; }

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxRate { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public decimal RemainingAmount => TotalAmount - PaidAmount;

    public InvoiceStatus Status { get; set; } = InvoiceStatus.Brouillon;
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }

    public ICollection<InvoiceItem> Items { get; set; } = new List<InvoiceItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
