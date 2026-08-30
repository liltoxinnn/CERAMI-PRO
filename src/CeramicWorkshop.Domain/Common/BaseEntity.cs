namespace CeramicWorkshop.Domain.Common;

/// <summary>
/// Entité de base : identifiant technique commun à toutes les tables.
/// </summary>
public abstract class BaseEntity
{
    public int Id { get; set; }
}
