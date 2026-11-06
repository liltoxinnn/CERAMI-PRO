using CeramiPro.Application.Common;
using CeramiPro.Domain.Common;

namespace CeramiPro.Application.DTOs.Finances;

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
    CommandesResumeDto Commandes,
    StockResumeDto Stock,
    FinancesResumeDto Finances,
    IReadOnlyList<PointGraphiqueDto> VentesParJour,
    IReadOnlyList<PointGraphiqueDto> VentesParMois,
    IReadOnlyList<ClassementDto> ProduitsLesPlusVendus,
    IReadOnlyList<ProduitAlerteDto> StockFaible);

/// <param name="ChiffreAffaires">Ventes du jour et commandes livrées ce jour-là.</param>
/// <param name="Encaisse">Argent reçu dans la journée, acomptes compris.</param>
public record ActiviteDuJourDto(
    decimal ChiffreAffaires, int NombreVentes, int CommandesLivrees, decimal Encaisse);

/// <param name="Benefice">Chiffre d'affaires, moins la TVA et le coût des pièces vendues.</param>
/// <param name="Resultat">Bénéfice moins les dépenses du mois.</param>
public record ActivitePeriodeDto(
    decimal ChiffreAffaires, decimal Benefice, decimal Depenses, decimal Resultat,
    int NombreVentes, int CommandesLivrees, decimal Encaisse);

public record CommandesResumeDto(
    int EnAttente, int EnCours, int ProchesEcheance, int EnRetard, int PretesALivrer);

public record StockResumeDto(
    int NombreProduits, int ProduitsFaibles, decimal ValeurStock);

/// <param name="ResteAEncaisser">Ventes à crédit et commandes non soldées.</param>
/// <param name="AcomptesRecus">Acomptes versés sur les commandes pas encore livrées.</param>
public record FinancesResumeDto(
    decimal ResteAEncaisser, decimal AcomptesRecus);

/// <summary>Produit passé sous son seuil, montré directement sur le tableau de bord.</summary>
public record ProduitAlerteDto(int Id, string Nom, decimal Stock, decimal Seuil);

/// <summary>Point d'un graphique : une étiquette et une valeur.</summary>
public record PointGraphiqueDto(string Etiquette, decimal Valeur);

/// <summary>Ligne de classement : produit ou matière, avec sa quantité et son montant.</summary>
public record ClassementDto(string Nom, decimal Quantite, decimal Montant, string? Unite);

// ------------------------------------------------------------------ Rapports

/// <summary>Rapports disponibles dans l'écran « Rapports ».</summary>
public enum TypeRapport
{
    [Libelle("Ventes par jour")] VentesParJour,
    [Libelle("Ventes par mois")] VentesParMois,
    [Libelle("Bénéfices")] Benefices,
    [Libelle("Dépenses")] Depenses,
    [Libelle("Dettes clients")] DettesClients,
    [Libelle("Produits les plus vendus")] ProduitsLesPlusVendus,
    [Libelle("Produits les plus rentables")] ProduitsLesPlusRentables,
    [Libelle("État du stock")] ValeurStock
}

/// <summary>Rapport prêt à afficher, à imprimer ou à exporter.</summary>
/// <param name="Remarque">Précision sur la façon dont les chiffres sont calculés.</param>
public record RapportDto(
    TypeRapport Type,
    string Titre,
    string Periode,
    IReadOnlyList<string> Colonnes,
    IReadOnlyList<IReadOnlyList<string>> Lignes,
    IReadOnlyList<string>? Totaux,
    IReadOnlyList<PointGraphiqueDto>? Graphique,
    string? Remarque = null);

public class RapportRequete
{
    public TypeRapport Type { get; set; } = TypeRapport.VentesParJour;
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}
