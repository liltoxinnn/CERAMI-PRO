using CeramicWorkshop.Application.Common;

namespace CeramicWorkshop.Application.DTOs.Finances;

// ------------------------------------------------------------------ Dépenses

public record DepenseDto(
    int Id,
    string Reference,
    int CategorieId,
    string CategorieNom,
    decimal Montant,
    DateTime Date,
    string Description,
    string? Justificatif,
    int? ModeReglementId,
    string? ModeReglement,
    string? Utilisateur);

public class DepenseRequete
{
    public int CategorieId { get; set; }
    public decimal Montant { get; set; }
    public DateTime? Date { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Justificatif { get; set; }
    public int? ModeReglementId { get; set; }
}

public class FiltreDepensesRequete : PagedRequest
{
    public int? CategorieId { get; set; }
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}

// ------------------------------------------------------------ Tableau de bord

/// <summary>Chiffres et graphiques affichés sur le tableau de bord.</summary>
public record TableauDeBordDto(
    ActiviteDuJourDto Aujourdhui,
    ActivitePeriodeDto Mois,
    ProductionResumeDto Production,
    CommandesResumeDto Commandes,
    StockResumeDto Stock,
    FinancesResumeDto Finances,
    IReadOnlyList<PointGraphiqueDto> VentesParJour,
    IReadOnlyList<PointGraphiqueDto> VentesParMois,
    IReadOnlyList<PointGraphiqueDto> BeneficesParMois,
    IReadOnlyList<PointGraphiqueDto> ProductionParMois,
    IReadOnlyList<ClassementDto> ProduitsLesPlusVendus,
    IReadOnlyList<ClassementDto> ProduitsLesPlusRentables,
    IReadOnlyList<ClassementDto> MatieresLesPlusConsommees);

public record ActiviteDuJourDto(
    decimal ChiffreAffaires, decimal Benefice, int NombreVentes, decimal PaiementsRecus);

public record ActivitePeriodeDto(
    decimal ChiffreAffaires, decimal Benefice, decimal Depenses, decimal Resultat, int NombreVentes);

public record ProductionResumeDto(
    int EnCours, int EnSechage, int EnAttenteCuisson, int EnDecoration,
    int EnControleQualite, int Terminees, int EnRetard);

public record CommandesResumeDto(
    int EnAttente, int EnCours, int ProchesEcheance, int EnRetard, int PretesALivrer);

public record StockResumeDto(
    int MatieresFaibles, int ProduitsFaibles, decimal ValeurMatieres,
    decimal ValeurProduits, decimal ValeurTotale);

public record FinancesResumeDto(
    decimal ArgentRecu, decimal CreancesClients, decimal DettesFournisseurs, decimal DepensesMois);

/// <summary>Point d'un graphique : une étiquette et une valeur.</summary>
public record PointGraphiqueDto(string Etiquette, decimal Valeur);

/// <summary>Ligne de classement : produit ou matière, avec sa quantité et son montant.</summary>
public record ClassementDto(string Nom, decimal Quantite, decimal Montant, string? Unite);

// ------------------------------------------------------------------ Rapports

/// <summary>Rapports disponibles dans l'écran « Rapports ».</summary>
public enum TypeRapport
{
    ChiffreAffaires,
    Benefices,
    Depenses,
    DettesClients,
    DettesFournisseurs,
    ConsommationMatieres,
    Production,
    ProduitsEndommages,
    ProduitsLesPlusVendus,
    ProduitsLesPlusRentables,
    ValeurStock,
    PerformanceProduction
}

/// <summary>Rapport prêt à afficher, à imprimer ou à exporter.</summary>
public record RapportDto(
    TypeRapport Type,
    string Titre,
    string Periode,
    IReadOnlyList<string> Colonnes,
    IReadOnlyList<IReadOnlyList<string>> Lignes,
    IReadOnlyList<string>? Totaux,
    IReadOnlyList<PointGraphiqueDto>? Graphique);

public class RapportRequete
{
    public TypeRapport Type { get; set; } = TypeRapport.ChiffreAffaires;
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}

// ------------------------------------------------------------- Calculateurs

/// <summary>Calcul d'une surface avec pourcentage de perte.</summary>
public class CalculSurfaceRequete
{
    public decimal Longueur { get; set; }
    public decimal Largeur { get; set; }
    public decimal PourcentagePerte { get; set; }
    public int NombrePieces { get; set; } = 1;
}

public record CalculSurfaceDto(
    decimal SurfaceUnitaire,
    decimal SurfaceTotale,
    decimal Perte,
    decimal SurfaceAvecPerte);

/// <summary>Calcul du nombre de pièces ou d'emballages nécessaires.</summary>
public class CalculQuantiteRequete
{
    public decimal QuantiteParUnite { get; set; } = 1m;
    public decimal QuantiteSouhaitee { get; set; }
    public decimal PourcentagePerte { get; set; }
}

public record CalculQuantiteDto(
    decimal QuantiteNecessaire,
    decimal QuantiteAvecPerte,
    int UnitesNecessaires);
