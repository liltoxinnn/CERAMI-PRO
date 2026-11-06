using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Sales;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Inventory;

/// <summary>
/// Mouvement de stock.
///
/// Aucune quantité ne change silencieusement : chaque variation laisse une
/// trace, avec le stock avant et après, et le document qui l'a provoquée.
/// C'est ce qui permet d'expliquer un écart d'inventaire plutôt que de le
/// constater.
/// </summary>
public class InventoryTransaction : AuditableEntity
{
    public InventoryTransactionType TransactionType { get; set; }

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>Quantité signée : positive pour une entrée, négative pour une sortie.</summary>
    public decimal Quantity { get; set; }
    public decimal QuantityBefore { get; set; }
    public decimal QuantityAfter { get; set; }

    public decimal UnitCost { get; set; }
    public decimal TotalCost { get; set; }

    public DateTime OccurredAt { get; set; }

    // Document à l'origine du mouvement, quand il y en a un.
    public int? SaleId { get; set; }
    public Sale? Sale { get; set; }

    public int? CustomOrderId { get; set; }
    public CustomOrder? CustomOrder { get; set; }

    /// <summary>Mouvement annulé par celui-ci, lors d'une inversion.</summary>
    public int? ReversedTransactionId { get; set; }
    public InventoryTransaction? ReversedTransaction { get; set; }

    /// <summary>Motif, obligatoire pour une régularisation après comptage.</summary>
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public int? UserId { get; set; }
    public User? User { get; set; }
}
