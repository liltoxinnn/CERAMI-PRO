using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;

namespace CeramiPro.Domain.Entities.Recipes;

/// <summary>
/// Recette de fabrication d'un produit : quantités de matières pour un nombre de pièces donné.
/// </summary>
public class ProductRecipe : AuditableEntity
{
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public string Name { get; set; } = null!;
    public int Version { get; set; } = 1;

    /// <summary>Nombre de pièces obtenues avec les quantités décrites dans la recette.</summary>
    public decimal YieldQuantity { get; set; } = 1m;

    public decimal LaborCost { get; set; }
    public decimal FiringCost { get; set; }
    public decimal DecorationCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal OtherCost { get; set; }

    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string? Notes { get; set; }

    public ICollection<ProductRecipeItem> Items { get; set; } = new List<ProductRecipeItem>();
}
