using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Materials;
using CeramiPro.Domain.Entities.Production;
using CeramiPro.Domain.Entities.Purchasing;
using CeramiPro.Domain.Entities.Sales;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Inventory;

/// <summary>
/// Mouvement de stock. Règle métier n°2 : aucun changement de stock n'est effectué
/// silencieusement, chaque variation laisse une trace avec le stock avant et après.
/// </summary>
public class InventoryTransaction : AuditableEntity
{
    public InventoryItemType ItemType { get; set; }
    public InventoryTransactionType TransactionType { get; set; }

    public int? MaterialId { get; set; }
    public Material? Material { get; set; }

    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public int? ProductVariantId { get; set; }
    public ProductVariant? ProductVariant { get; set; }

    public int? MaterialBatchId { get; set; }
    public MaterialBatch? MaterialBatch { get; set; }

    /// <summary>Quantité signée : positive pour une entrée, négative pour une sortie.</summary>
    public decimal Quantity { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }

    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public DateTime OccurredAt { get; set; }

    // Document à l'origine du mouvement.
    public int? PurchaseId { get; set; }
    public Purchase? Purchase { get; set; }

    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int? ProductionOrderId { get; set; }
    public ProductionOrder? ProductionOrder { get; set; }

    public int? StockAdjustmentId { get; set; }
    public StockAdjustment? StockAdjustment { get; set; }

    /// <summary>Mouvement annulé par celui-ci (règle métier n°6 : inversion propre).</summary>
    public int? ReversedTransactionId { get; set; }
    public InventoryTransaction? ReversedTransaction { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
}
