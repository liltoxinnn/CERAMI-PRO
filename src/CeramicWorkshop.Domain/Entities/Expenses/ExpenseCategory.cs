using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Entities.Expenses;

/// <summary>Catégorie de dépense : électricité, gaz, transport, salaires…</summary>
public class ExpenseCategory : AuditableEntity
{
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
}
