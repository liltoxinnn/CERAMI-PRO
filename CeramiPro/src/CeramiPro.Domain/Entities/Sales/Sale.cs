using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Invoicing;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Sales;

/// <summary>Vente de produits finis. Une vente confirmée diminue le stock (règle métier n°3).</summary>
public class Sale : AuditableEntity, ISoftDeletable
{
    public string SaleNumber { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public DateTime SaleDate { get; set; }
    public SaleStatus Status { get; set; } = SaleStatus.Brouillon;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TaxAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Reste à payer sur la vente.</summary>
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    /// <summary>Coût de revient total des articles vendus, pour le calcul du bénéfice.</summary>
    public decimal TotalCost { get; set; }

    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }

    public ICollection<SaleItem> Items { get; set; } = new List<SaleItem>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    public ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
}
