using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Entities.Settings;

/// <summary>Réglage technique clé/valeur (sauvegardes, seuils, options d'affichage).</summary>
public class SystemSetting : AuditableEntity
{
    public string Key { get; set; } = null!;
    public string? Value { get; set; }
    public string Category { get; set; } = "Général";
    public string ValueType { get; set; } = "texte";
    public string? Description { get; set; }

    /// <summary>Réglage modifiable uniquement par un administrateur.</summary>
    public bool IsAdminOnly { get; set; }
}
