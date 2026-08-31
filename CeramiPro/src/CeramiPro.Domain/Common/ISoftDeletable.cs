namespace CeramiPro.Domain.Common;

/// <summary>
/// Marque une entité dont la suppression doit rester réversible. Les pièces
/// comptables — ventes, factures, paiements, dépenses — ne sont jamais
/// effacées : elles sont marquées comme supprimées et restent consultables.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    int? DeletedByUserId { get; set; }
    string? DeletionReason { get; set; }
}
