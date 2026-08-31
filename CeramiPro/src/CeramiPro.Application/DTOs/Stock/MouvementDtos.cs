using CeramiPro.Application.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Stock;

/// <summary>Demande d'enregistrement d'un mouvement de stock.</summary>
public class MouvementStockRequete
{
    public InventoryItemType TypeArticle { get; set; }
    public InventoryTransactionType TypeMouvement { get; set; }

    public int? MatiereId { get; set; }
    public int? ProduitId { get; set; }
    public int? VarianteId { get; set; }
    public int? LotId { get; set; }

    /// <summary>Quantité signée : positive pour une entrée, négative pour une sortie.</summary>
    public decimal Quantite { get; set; }

    public decimal CoutUnitaire { get; set; }
    public DateTime? Date { get; set; }

    public int? AchatId { get; set; }
    public int? VenteId { get; set; }
    public int? ProductionId { get; set; }
    public int? AjustementId { get; set; }
    public int? MouvementAnnuleId { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    /// <summary>Dérogation d'un administrateur autorisant un stock négatif.</summary>
    public bool AutoriserStockNegatif { get; set; }
}

/// <summary>Mouvement affiché dans l'écran « Mouvements de stock ».</summary>
public record MouvementStockDto(
    int Id,
    DateTime Date,
    string TypeArticle,
    string TypeMouvement,
    string Article,
    string? ReferenceArticle,
    string Unite,
    decimal Quantite,
    decimal StockAvant,
    decimal StockApres,
    decimal CoutUnitaire,
    decimal CoutTotal,
    string? Document,
    string? Utilisateur,
    string? Notes);

/// <summary>Filtres de l'écran des mouvements de stock.</summary>
public class FiltreMouvementsRequete : PagedRequest
{
    public InventoryItemType? TypeArticle { get; set; }
    public InventoryTransactionType? TypeMouvement { get; set; }
    public int? MatiereId { get; set; }
    public int? ProduitId { get; set; }
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}

/// <summary>Régularisation de stock après comptage physique.</summary>
public class RegularisationRequete
{
    public InventoryItemType TypeArticle { get; set; }
    public int? MatiereId { get; set; }
    public int? ProduitId { get; set; }
    public decimal QuantiteComptee { get; set; }
    public StockAdjustmentReason Motif { get; set; }
    public string? Notes { get; set; }
}
