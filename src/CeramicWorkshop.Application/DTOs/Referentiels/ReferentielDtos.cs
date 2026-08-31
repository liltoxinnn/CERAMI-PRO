using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Application.DTOs.Referentiels;

/// <summary>Listes simples que l'atelier gère lui-même.</summary>
public enum TypeReferentiel
{
    [Libelle("Catégories de matières")] CategorieMatiere,
    [Libelle("Catégories de produits")] CategorieProduit,
    [Libelle("Catégories de dépenses")] CategorieDepense,
    [Libelle("Types de décoration")] TypeDecoration
}

/// <summary>Élément d'une liste : catégorie, type de décoration…</summary>
public record ElementReferentielDto(
    int Id,
    string Nom,
    string? Description,
    bool Actif,
    bool Systeme,
    int NombreUtilisations);

public class ElementReferentielRequete
{
    public string Nom { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool Actif { get; set; } = true;
}

/// <summary>Unité de mesure de l'atelier.</summary>
public record UniteDto(
    int Id,
    string Code,
    string Nom,
    UnitType Type,
    string TypeLibelle,
    decimal FacteurConversion,
    bool Systeme,
    bool Actif,
    int NombreUtilisations);

public class UniteRequete
{
    public string Code { get; set; } = string.Empty;
    public string Nom { get; set; } = string.Empty;
    public UnitType Type { get; set; } = UnitType.Quantite;
    public decimal FacteurConversion { get; set; } = 1m;
    public bool Actif { get; set; } = true;
}

/// <summary>Mode de règlement proposé lors d'un paiement.</summary>
public record ModeReglementDto(int Id, string Code, string Nom, bool ReferenceObligatoire, bool Actif);
