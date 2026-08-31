using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Payments;

namespace CeramiPro.Domain.Entities.Expenses;

public class Expense : AuditableEntity, ISoftDeletable
{
    public string Reference { get; set; } = null!;

    public int ExpenseCategoryId { get; set; }
    public ExpenseCategory ExpenseCategory { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string Description { get; set; } = null!;

    /// <summary>Chemin du justificatif numérisé.</summary>
    public string? ReceiptPath { get; set; }

    public int? PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }
}
