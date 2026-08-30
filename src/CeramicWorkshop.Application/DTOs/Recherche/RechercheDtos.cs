namespace CeramicWorkshop.Application.DTOs.Recherche;

/// <summary>Familles de fiches parcourues par la recherche globale.</summary>
public enum FamilleResultat
{
    Produit,
    Matiere,
    Client,
    Fournisseur,
    OrdreProduction,
    Commande,
    Vente,
    Facture,
    Achat,
    Depense
}

/// <summary>Une fiche trouvée par la recherche globale.</summary>
public record ResultatRechercheDto(
    FamilleResultat Famille,
    string FamilleLibelle,
    int Id,
    string Titre,
    string? SousTitre,
    string? Complement,
    string Adresse,
    int Pertinence);

/// <summary>Résultats regroupés par famille de fiches.</summary>
public record GroupeResultatsDto(
    FamilleResultat Famille,
    string Libelle,
    IReadOnlyList<ResultatRechercheDto> Resultats);

public record RechercheGlobaleDto(
    string Terme,
    int Total,
    IReadOnlyList<GroupeResultatsDto> Groupes);
