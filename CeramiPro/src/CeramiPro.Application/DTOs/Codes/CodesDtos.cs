namespace CeramiPro.Application.DTOs.Codes;

/// <summary>
/// Étiquette prête à imprimer : les images sont fournies en SVG, ce qui reste
/// net à l'impression quelle que soit la taille du papier.
/// </summary>
public record EtiquetteDto(
    int ProduitId,
    string Nom,
    string Reference,
    string Categorie,
    decimal PrixVente,
    string PrixAffiche,
    string CodeBarres,
    string CodeQr,
    string CodeBarresSvg,
    string CodeQrSvg);

/// <summary>Ce que le logiciel a reconnu derrière un code scanné.</summary>
public enum CibleScan
{
    Inconnu,
    Produit,
    Matiere,
    OrdreProduction,
    Commande,
    Vente,
    Facture,
    Achat
}

/// <summary>
/// Résultat d'un scan : la nature de la fiche trouvée et l'adresse de l'écran
/// à ouvrir. <see cref="Trouve"/> est faux quand le code ne correspond à rien.
/// </summary>
public record ResultatScanDto(
    bool Trouve,
    CibleScan Cible,
    int? Id,
    string Code,
    string Libelle,
    string? Details,
    string? Adresse);

/// <summary>Demande d'impression d'une planche d'étiquettes.</summary>
public class EtiquettesRequete
{
    public List<int> ProduitIds { get; set; } = new();

    public int Exemplaires { get; set; } = 1;
}
