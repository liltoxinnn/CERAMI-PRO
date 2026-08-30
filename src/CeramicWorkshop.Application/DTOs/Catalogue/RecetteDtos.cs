namespace CeramicWorkshop.Application.DTOs.Catalogue;

/// <summary>Recette de fabrication d'un produit.</summary>
public record RecetteDto(
    int Id,
    int ProduitId,
    string ProduitNom,
    string Nom,
    int Version,
    decimal Rendement,
    decimal CoutMainOeuvre,
    decimal CoutCuisson,
    decimal CoutDecoration,
    decimal CoutEmballage,
    decimal AutresCouts,
    decimal CoutMatieres,
    decimal CoutTotal,
    decimal CoutUnitaire,
    bool ParDefaut,
    bool Active,
    string? Notes,
    IReadOnlyList<LigneRecetteDto> Lignes);

public record LigneRecetteDto(
    int Id,
    int MatiereId,
    string MatiereNom,
    string MatiereReference,
    int UniteId,
    string UniteCode,
    decimal Quantite,
    decimal PourcentagePerte,
    decimal QuantiteAvecPerte,
    decimal CoutUnitaire,
    decimal Cout,
    string? Notes);

public class RecetteRequete
{
    public int ProduitId { get; set; }
    public string Nom { get; set; } = string.Empty;

    /// <summary>Nombre de pièces obtenues avec les quantités décrites.</summary>
    public decimal Rendement { get; set; } = 1m;

    public decimal CoutMainOeuvre { get; set; }
    public decimal CoutCuisson { get; set; }
    public decimal CoutDecoration { get; set; }
    public decimal CoutEmballage { get; set; }
    public decimal AutresCouts { get; set; }
    public bool ParDefaut { get; set; }
    public bool Active { get; set; } = true;
    public string? Notes { get; set; }
    public List<LigneRecetteRequete> Lignes { get; set; } = new();
}

public class LigneRecetteRequete
{
    public int MatiereId { get; set; }
    public int UniteId { get; set; }
    public decimal Quantite { get; set; }
    public decimal PourcentagePerte { get; set; }
    public string? Notes { get; set; }
}

/// <summary>
/// Calcul des matières nécessaires pour fabriquer une quantité donnée,
/// avec le stock disponible et ce qui manque éventuellement.
/// </summary>
public record BesoinsRecetteDto(
    int RecetteId,
    string RecetteNom,
    string ProduitNom,
    decimal QuantiteAProduire,
    decimal CoutMatieres,
    decimal CoutTotal,
    decimal CoutUnitaire,
    bool MatieresSuffisantes,
    IReadOnlyList<BesoinMatiereDto> Besoins);

public record BesoinMatiereDto(
    int MatiereId,
    string MatiereNom,
    string UniteCode,
    decimal QuantiteNecessaire,
    decimal QuantiteDisponible,
    decimal Manquant,
    decimal CoutUnitaire,
    decimal Cout,
    bool Suffisant);
