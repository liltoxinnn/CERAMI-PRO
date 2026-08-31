using CeramiPro.Application.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Production;

/// <summary>Ordre de production affiché dans la liste et le tableau de production.</summary>
public record OrdreProductionDto(
    int Id,
    string Numero,
    int ProduitId,
    string ProduitNom,
    string ProduitReference,
    int? RecetteId,
    string? RecetteNom,
    int? CommandeId,
    string? CommandeNumero,
    decimal QuantitePrevue,
    decimal QuantiteTerminee,
    decimal QuantiteEndommagee,
    Priority Priorite,
    string PrioriteLibelle,
    ProductionStatus Statut,
    string StatutLibelle,
    DateTime DateDebutPrevue,
    DateTime? DateFinPrevue,
    DateTime? DateDebutReelle,
    DateTime? DateFinReelle,
    int? EmployeId,
    string? EmployeNom,
    string? Notes,
    decimal CoutMatieresEstime,
    decimal CoutMatieresReel,
    decimal CoutMainOeuvre,
    decimal CoutCuisson,
    decimal CoutDecoration,
    decimal CoutEmballage,
    decimal AutresCouts,
    decimal CoutTotal,
    decimal CoutUnitaire,
    bool MatieresConsommees,
    bool DerogationStock,
    string? MotifDerogation,
    bool EnRetard,
    IReadOnlyList<MatiereProductionDto> Matieres,
    IReadOnlyList<EtapeProductionDto> Etapes);

public record MatiereProductionDto(
    int Id,
    int MatiereId,
    string MatiereNom,
    string UniteCode,
    decimal QuantitePrevue,
    decimal QuantiteConsommee,
    decimal CoutUnitaire,
    decimal Cout);

public record EtapeProductionDto(
    int Id,
    ProductionStatus Etape,
    string EtapeLibelle,
    DateTime Debut,
    DateTime? Fin,
    string? Employe,
    decimal QuantiteAcceptee,
    decimal QuantiteEndommagee,
    string? Notes);

public class OrdreProductionRequete
{
    public int ProduitId { get; set; }
    public int? RecetteId { get; set; }
    public int? CommandeId { get; set; }
    public decimal QuantitePrevue { get; set; }
    public Priority Priorite { get; set; } = Priority.Normale;
    public DateTime? DateDebutPrevue { get; set; }
    public DateTime? DateFinPrevue { get; set; }
    public int? EmployeId { get; set; }
    public string? Notes { get; set; }
    public decimal CoutMainOeuvre { get; set; }
    public decimal CoutEmballage { get; set; }
    public decimal AutresCouts { get; set; }
}

/// <summary>Passage d'une étape de fabrication à la suivante.</summary>
public class ChangementEtapeRequete
{
    public ProductionStatus NouvelleEtape { get; set; }
    public decimal QuantiteAcceptee { get; set; }
    public decimal QuantiteEndommagee { get; set; }
    public int? EmployeId { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Lancement de la production : les matières sortent du stock.</summary>
public class LancementProductionRequete
{
    /// <summary>Dérogation d'un administrateur en cas de matière insuffisante.</summary>
    public bool ForcerMalgreStockInsuffisant { get; set; }

    public string? MotifDerogation { get; set; }
}

public class FiltreProductionsRequete : PagedRequest
{
    public ProductionStatus? Statut { get; set; }
    public int? ProduitId { get; set; }
    public int? EmployeId { get; set; }
    public bool SeulementEnCours { get; set; }
    public bool SeulementEnRetard { get; set; }
}

/// <summary>Colonne du tableau de production, une par étape de fabrication.</summary>
public record ColonneProductionDto(
    ProductionStatus Etape,
    string EtapeLibelle,
    decimal QuantiteTotale,
    IReadOnlyList<OrdreProductionDto> Ordres);

/// <summary>Chiffres clés de la production affichés sur le tableau de bord.</summary>
public record SyntheseProductionDto(
    int EnCours,
    int EnSechage,
    int EnAttenteCuisson,
    int EnDecoration,
    int EnControleQualite,
    int Terminees,
    int EnRetard,
    decimal PiecesEnFabrication);
