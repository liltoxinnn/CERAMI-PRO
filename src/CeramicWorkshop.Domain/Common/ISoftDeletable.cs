namespace CeramicWorkshop.Domain.Common;

/// <summary>
/// Règle métier n°15 : les transactions financières importantes ne sont jamais
/// supprimées physiquement. Elles sont marquées comme supprimées et restent auditables.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; set; }
    DateTime? DeletedAt { get; set; }
    int? DeletedByUserId { get; set; }
}
