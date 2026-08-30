using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Entities.Payments;
using CeramicWorkshop.Domain.Entities.Purchasing;

namespace CeramicWorkshop.Domain.Entities.Suppliers;

/// <summary>Paiement versé à un fournisseur (règlement d'achat ou de dette).</summary>
public class SupplierPayment : AuditableEntity, ISoftDeletable
{
    public string PaymentNumber { get; set; } = null!;

    public int SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }
}
