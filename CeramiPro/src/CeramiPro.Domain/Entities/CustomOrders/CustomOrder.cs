using CeramiPro.Domain.Common;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Payments;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Domain.Entities.CustomOrders;

/// <summary>
/// Commande personnalisée passée par un client (pièce unique ou série sur mesure).
/// Règle métier n°16 : une date limite est obligatoire.
/// </summary>
public class CustomOrder : AuditableEntity, ISoftDeletable
{
    public string OrderNumber { get; set; } = null!;

    public int CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public string Title { get; set; } = null!;

    /// <summary>Par où la commande est arrivée : atelier, Instagram, WhatsApp…</summary>
    public CanalCommande Channel { get; set; } = CanalCommande.Atelier;
    public string? Description { get; set; }

    public decimal? Width { get; set; }
    public decimal? Height { get; set; }
    public decimal? Depth { get; set; }
    public string? Colors { get; set; }
    public string? Materials { get; set; }

    public decimal Quantity { get; set; } = 1m;
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal PaidAmount { get; set; }

    /// <summary>Reste à payer (règle métier n°13 : calculé automatiquement).</summary>
    public decimal RemainingAmount => TotalAmount - PaidAmount;

    public DateTime OrderDate { get; set; }
    public DateTime Deadline { get; set; }
    public DateTime? DeliveredAt { get; set; }

    public CustomOrderStatus Status { get; set; } = CustomOrderStatus.Recue;

    public int? AssignedUserId { get; set; }
    public User? AssignedUser { get; set; }

    public string? Notes { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAt { get; set; }
    public int? DeletedByUserId { get; set; }

    /// <summary>Motif saisi lors de la suppression, conservé pour l\'historique.</summary>
    public string? DeletionReason { get; set; }

    /// <summary>Jeton de concurrence (« xmin ») : protège le montant déjà encaissé.</summary>
    public uint Version { get; set; }

    public ICollection<CustomOrderImage> Images { get; set; } = new List<CustomOrderImage>();
    public ICollection<CustomOrderNote> OrderNotes { get; set; } = new List<CustomOrderNote>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
