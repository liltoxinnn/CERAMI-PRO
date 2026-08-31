using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Materials;

/// <summary>Unité de mesure (kg, g, L, ml, pièce, m, boîte, ou unité personnalisée).</summary>
public class Unit : AuditableEntity
{
    public string Code { get; set; } = null!;
    public string Name { get; set; } = null!;
    public UnitType Type { get; set; }

    /// <summary>Facteur de conversion vers l'unité de référence de la famille (ex. g -> kg = 0,001).</summary>
    public decimal ConversionFactor { get; set; } = 1m;

    public bool IsSystem { get; set; }
    public bool IsActive { get; set; } = true;

    public ICollection<Material> Materials { get; set; } = new List<Material>();
}
