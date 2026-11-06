using CeramiPro.Domain.Common;

namespace CeramiPro.Application.DTOs.Referentiels;

/// <summary>Listes simples que l'atelier gère lui-même.</summary>
public enum TypeReferentiel
{
    [Libelle("Catégories de produits")] CategorieProduit,
    [Libelle("Catégories de dépenses")] CategorieDepense
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

/// <summary>Mode de règlement proposé lors d'un paiement.</summary>
public record ModeReglementDto(int Id, string Code, string Nom, bool ReferenceObligatoire, bool Actif);
