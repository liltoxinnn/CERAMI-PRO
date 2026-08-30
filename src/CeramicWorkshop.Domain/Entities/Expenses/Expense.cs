using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Identity;
using CeramicWorkshop.Domain.Entities.Payments;

namespace CeramicWorkshop.Domain.Entities.Expenses;

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
}
