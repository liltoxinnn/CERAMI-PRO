using CeramiPro.Application.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Stock;

/// <summary>Demande d'enregistrement d'un mouvement de stock.</summary>
public class MouvementStockRequete
{
    public InventoryTransactionType TypeMouvement { get; set; }

    public int ProduitId { get; set; }

    /// <summary>Quantité signée : positive pour une entrée, négative pour une sortie.</summary>
    public decimal Quantite { get; set; }

    public decimal CoutUnitaire { get; set; }
    public DateTime? Date { get; set; }

    public int? VenteId { get; set; }
    public int? CommandeId { get; set; }
    public int? MouvementAnnuleId { get; set; }

    public string? Reference { get; set; }
    public string? Notes { get; set; }

    /// <summary>Dérogation d'un administrateur autorisant un stock négatif.</summary>
    public bool AutoriserStockNegatif { get; set; }
}

/// <summary>Mouvement affiché dans l'historique du stock.</summary>
public record MouvementStockDto(
    int Id,
    DateTime Date,
    string TypeMouvement,
    string Produit,
    string? ReferenceProduit,
    decimal Quantite,
    decimal StockAvant,
    decimal StockApres,
    decimal CoutUnitaire,
    decimal CoutTotal,
    string? Document,
    string? Utilisateur,
    string? Notes);

/// <summary>Filtres de l'historique des mouvements.</summary>
public class FiltreMouvementsRequete : PagedRequest
{
    public InventoryTransactionType? TypeMouvement { get; set; }
    public int? ProduitId { get; set; }
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}

/// <summary>
/// Régularisation après comptage physique. Le motif est obligatoire : un
/// écart de stock corrigé sans explication ne s'analyse plus ensuite.
/// </summary>
public class RegularisationRequete
{
    public int ProduitId { get; set; }
    public decimal QuantiteComptee { get; set; }
    public string Motif { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
