using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Catalog;
using CeramiPro.Domain.Entities.CustomOrders;
using CeramiPro.Domain.Entities.Decoration;
using CeramiPro.Domain.Entities.Firing;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Inventory;
using CeramiPro.Domain.Entities.Quality;
using CeramiPro.Domain.Entities.Recipes;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.Production;

/// <summary>
/// Ordre de production : suit une série de pièces du façonnage au produit fini.
/// Le passage au statut « Terminé » exige un contrôle qualité (règle métier n°10).
/// </summary>
public class ProductionOrder : AuditableEntity, ISoftDeletable
{
    public string ProductionNumber { get; set; } = null!;

    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int? ProductRecipeId { get; set; }
    public ProductRecipe? ProductRecipe { get; set; }

    /// <summary>Production déclenchée par une commande personnalisée, le cas échéant.</summary>
    public int? CustomOrderId { get; set; }
    public CustomOrder? CustomOrder { get; set; }

    public decimal PlannedQuantity { get; set; }
    public decimal CompletedQuantity { get; set; }
    public decimal DamagedQuantity { get; set; }

    public Priority Priority { get; set; } = Priority.Normale;
    public ProductionStatus Status { get; set; } = ProductionStatus.Planifie;

    public DateTime PlannedStartDate { get; set; }
    public DateTime? PlannedEndDate { get; set; }
    public DateTime? ActualStartDate { get; set; }
    public DateTime? ActualEndDate { get; set; }

    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public string? Notes { get; set; }

    // Coûts : estimés à la création, réels au fur et à mesure de l'avancement.
    public decimal EstimatedMaterialCost { get; set; }
    public decimal ActualMaterialCost { get; set; }
    public decimal LaborCost { get; set; }
    public decimal FiringCost { get; set; }
    public decimal DecorationCost { get; set; }
    public decimal PackagingCost { get; set; }
    public decimal OtherCost { get; set; }

    /// <summary>Coût réel total de la série.</summary>
    public decimal TotalCost => ActualMaterialCost + LaborCost + FiringCost + DecorationCost + PackagingCost + OtherCost;

    /// <summary>Coût de revient d'une pièce acceptée.</summary>
    public decimal UnitCost => CompletedQuantity > 0 ? TotalCost / CompletedQuantity : 0m;

    public bool MaterialsConsumed { get; set; }

    /// <summary>Règle métier n°11 : dérogation administrateur sur le contrôle de stock.</summary>
    public bool StockCheckOverridden { get; set; }
    public int? OverriddenByUserId { get; set; }
    public string? OverrideReason { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }

    public ICollection<ProductionMaterial> Materials { get; set; } = new List<ProductionMaterial>();
    public ICollection<ProductionStageHistory> StageHistory { get; set; } = new List<ProductionStageHistory>();
    public ICollection<FiringBatchItem> FiringBatchItems { get; set; } = new List<FiringBatchItem>();
    public ICollection<DecorationOrder> DecorationOrders { get; set; } = new List<DecorationOrder>();
    public ICollection<QualityCheck> QualityChecks { get; set; } = new List<QualityCheck>();
    public ICollection<InventoryTransaction> InventoryTransactions { get; set; } = new List<InventoryTransaction>();
}
