using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;

namespace CeramiPro.Domain.Entities.Sales;

public class SaleItem : BaseEntity
{
    public int SaleId { get; set; }
    public Sale Sale { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public string Description { get; set; } = null!;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }

    /// <summary>Coût de revient unitaire au moment de la vente (figé pour le calcul du bénéfice).</summary>
    public decimal UnitCost { get; set; }
}
