using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Suppliers;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Purchasing;

/// <summary>Achat de matières premières auprès d'un fournisseur.</summary>
public class Purchase : AuditableEntity, ISoftDeletable
{
    public string PurchaseNumber { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public DateTime PurchaseDate { get; set; }
    public PurchaseStatus Status { get; set; } = PurchaseStatus.Brouillon;

    public decimal Subtotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Reste à payer au fournisseur (dette).</summary>
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    public string? InvoiceReference { get; set; }
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }

    public ICollection<PurchaseItem> Items { get; set; } = new List<PurchaseItem>();
    public ICollection<SupplierPayment> Payments { get; set; } = new List<SupplierPayment>();
}
