using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Materials;

namespace CeramicWorkshop.Domain.Entities.Recipes;

public class ProductRecipeItem : BaseEntity
{
    public int ProductRecipeId { get; set; }
    public ProductRecipe ProductRecipe { get; set; } = null!;

    public int MaterialId { get; set; }
    public Material Material { get; set; } = null!;

    public int UnitId { get; set; }
    public Unit Unit { get; set; } = null!;

    public decimal Quantity { get; set; }

    /// <summary>Pourcentage de perte prévu sur cette matière (casse, chutes, évaporation).</summary>
    public decimal WastePercentage { get; set; }

    public string? Notes { get; set; }
}
