using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Sales;

namespace CeramiPro.Domain.Entities.Payments;

/// <summary>
/// Encaissement client : paiement complet, partiel, acompte ou règlement de dette.
///
/// Chaque paiement est enregistré individuellement et n'est jamais supprimé
/// définitivement : l'historique d'un règlement est la seule preuve de ce qui
/// a été reçu.
/// </summary>
public class Payment : AuditableEntity, ISoftDeletable
{
    public string PaymentNumber { get; set; } = null!;

    public int? CustomerId { get; set; }
    public Customer? Customer { get; set; }

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int? CustomOrderId { get; set; }
    public CustomOrder? CustomOrder { get; set; }

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }

    public int PaymentMethodId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = null!;

    /// <summary>Acompte versé à la commande.</summary>
    public bool IsDeposit { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }
}
