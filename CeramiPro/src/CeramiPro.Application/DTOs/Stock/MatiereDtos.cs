using CeramiPro.Application.Common;

namespace CeramiPro.Application.DTOs.Stock;

/// <summary>Matière première telle qu'elle apparaît dans la liste et la fiche.</summary>
public record MatiereDto(
    int Id,
    string Reference,
    string Nom,
    int CategorieId,
    string CategorieNom,
    int UniteId,
    string UniteCode,
    decimal QuantiteActuelle,
    decimal StockMinimum,
    decimal? StockMaximum,
    decimal CoutMoyen,
    decimal PrixDernierAchat,
    int? FournisseurId,
    string? FournisseurNom,
    string? Emplacement,
    string? Description,
    string? Image,
    bool Actif,
    decimal ValeurStock,
    bool StockFaible);

public class MatiereRequete
{
    public string Nom { get; set; } = string.Empty;
    public int CategorieId { get; set; }
    public int UniteId { get; set; }
    public decimal StockMinimum { get; set; }
    public decimal? StockMaximum { get; set; }
    public decimal PrixAchat { get; set; }
    public int? FournisseurId { get; set; }
    public string? Emplacement { get; set; }
    public string? Description { get; set; }
    public string? Image { get; set; }
    public bool Actif { get; set; } = true;

    /// <summary>Quantité déjà présente dans l'atelier au moment de la création de la fiche.</summary>
    public decimal StockInitial { get; set; }
}

public class FiltreMatieresRequete : PagedRequest
{
    public int? CategorieId { get; set; }
    public int? FournisseurId { get; set; }
    public bool SeulementStockFaible { get; set; }
    public bool InclureInactives { get; set; } = true;
}

/// <summary>Lot de matière reçu, utilisé pour la traçabilité.</summary>
public record LotMatiereDto(
    int Id,
    string Numero,
    int MatiereId,
    string MatiereNom,
    decimal Quantite,
    decimal QuantiteRestante,
    decimal CoutUnitaire,
    DateTime DateReception,
    DateTime? DatePeremption,
    string? Emplacement,
    string? Notes);

/// <summary>Synthèse affichée en haut de l'écran des matières premières.</summary>
public record SyntheseStockDto(
    int NombreArticles,
    int NombreStockFaible,
    decimal ValeurTotale);
