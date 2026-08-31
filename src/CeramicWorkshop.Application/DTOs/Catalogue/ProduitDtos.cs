using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Application.DTOs.Catalogue;

/// <summary>Produit céramique du catalogue.</summary>
public record ProduitDto(
    int Id,
    string Reference,
    string Nom,
    int CategorieId,
    string CategorieNom,
    string? Description,
    string? Matiere,
    string? Couleur,
    string? Finition,
    decimal? Largeur,
    decimal? Hauteur,
    decimal? Profondeur,
    decimal? Poids,
    decimal CoutProduction,
    decimal PrixVente,
    decimal Marge,
    decimal TauxMarge,
    decimal StockActuel,
    decimal StockMinimum,
    string? CodeBarres,
    string? QrCode,
    bool Personnalisable,
    bool Actif,
    bool StockFaible,
    string? ImagePrincipale,
    int NombrePhotos,
    int NombreVariantes,
    int NombreRecettes);

public class ProduitRequete
{
    public string Nom { get; set; } = string.Empty;
    public int CategorieId { get; set; }
    public string? Description { get; set; }
    public string? Matiere { get; set; }
    public string? Couleur { get; set; }
    public string? Finition { get; set; }
    public decimal? Largeur { get; set; }
    public decimal? Hauteur { get; set; }
    public decimal? Profondeur { get; set; }
    public decimal? Poids { get; set; }
    public decimal CoutProduction { get; set; }
    public decimal PrixVente { get; set; }
    public decimal StockMinimum { get; set; }
    public string? CodeBarres { get; set; }
    public bool Personnalisable { get; set; }
    public bool Actif { get; set; } = true;

    /// <summary>Pièces déjà présentes dans l'atelier lors de la création de la fiche.</summary>
    public decimal StockInitial { get; set; }
}

public class FiltreProduitsRequete : PagedRequest
{
    public int? CategorieId { get; set; }
    public bool SeulementStockFaible { get; set; }
    public bool SeulementPersonnalisables { get; set; }
    public bool InclureInactifs { get; set; } = true;
}

/// <summary>Photo associée à un produit.</summary>
public record PhotoProduitDto(
    int Id,
    int ProduitId,
    string Chemin,
    string? Legende,
    ProductImageKind Type,
    string TypeLibelle,
    bool Principale,
    int Ordre);

public class PhotoProduitRequete
{
    public string Chemin { get; set; } = string.Empty;
    public string? Legende { get; set; }
    public ProductImageKind Type { get; set; } = ProductImageKind.Supplementaire;
    public bool Principale { get; set; }
}

/// <summary>Déclinaison d'un produit (taille, couleur).</summary>
public record VarianteProduitDto(
    int Id,
    int ProduitId,
    string Reference,
    string Nom,
    string? Couleur,
    string? Taille,
    decimal AjustementPrix,
    decimal PrixFinal,
    decimal StockActuel,
    decimal StockMinimum,
    string? CodeBarres,
    bool Actif);

public class VarianteProduitRequete
{
    public string Nom { get; set; } = string.Empty;
    public string? Couleur { get; set; }
    public string? Taille { get; set; }
    public decimal AjustementPrix { get; set; }
    public decimal StockMinimum { get; set; }
    public string? CodeBarres { get; set; }
    public bool Actif { get; set; } = true;
}

/// <summary>Synthèse du catalogue affichée en haut de l'écran des produits.</summary>
public record SyntheseCatalogueDto(
    int NombreProduits,
    int NombreStockFaible,
    decimal ValeurStock,
    decimal MargeMoyenne);
