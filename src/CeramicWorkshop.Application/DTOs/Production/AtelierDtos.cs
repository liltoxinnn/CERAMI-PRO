using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Application.DTOs.Production;

// ---------------------------------------------------------------- Fours

public record FourDto(
    int Id,
    string Reference,
    string Nom,
    decimal Capacite,
    decimal TemperatureMin,
    decimal TemperatureMax,
    string? Emplacement,
    KilnStatus Statut,
    string StatutLibelle,
    string? Notes,
    bool Actif,
    int CuissonsEnCours);

public class FourRequete
{
    public string Nom { get; set; } = string.Empty;
    public decimal Capacite { get; set; }
    public decimal TemperatureMin { get; set; }
    public decimal TemperatureMax { get; set; }
    public string? Emplacement { get; set; }
    public KilnStatus Statut { get; set; } = KilnStatus.Disponible;
    public string? Notes { get; set; }
    public bool Actif { get; set; } = true;
}

// -------------------------------------------------------------- Cuissons

public record CuissonDto(
    int Id,
    string Numero,
    int FourId,
    string FourNom,
    FiringType Type,
    string TypeLibelle,
    FiringBatchStatus Statut,
    string StatutLibelle,
    decimal Temperature,
    DateTime Debut,
    DateTime? Fin,
    decimal? DureeHeures,
    decimal CoutEnergie,
    decimal QuantiteEndommagee,
    string? Observations,
    string? Utilisateur,
    decimal QuantiteTotale,
    IReadOnlyList<PieceCuissonDto> Pieces);

public record PieceCuissonDto(
    int Id,
    int? ProductionId,
    string? ProductionNumero,
    int ProduitId,
    string ProduitNom,
    decimal Quantite,
    decimal QuantiteAcceptee,
    decimal QuantiteEndommagee,
    decimal CoutEnergieImpute,
    string? Notes);

public class CuissonRequete
{
    public int FourId { get; set; }
    public FiringType Type { get; set; } = FiringType.PremiereCuisson;
    public decimal Temperature { get; set; }
    public DateTime? Debut { get; set; }
    public decimal CoutEnergie { get; set; }
    public string? Observations { get; set; }
    public List<PieceCuissonRequete> Pieces { get; set; } = new();
}

public class PieceCuissonRequete
{
    public int? ProductionId { get; set; }
    public int ProduitId { get; set; }
    public decimal Quantite { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Défournement : ce qui sort intact et ce qui est cassé.</summary>
public class DefournementRequete
{
    public DateTime? Fin { get; set; }
    public decimal CoutEnergie { get; set; }
    public string? Observations { get; set; }
    public List<ResultatPieceRequete> Pieces { get; set; } = new();
}

public class ResultatPieceRequete
{
    public int PieceId { get; set; }
    public decimal QuantiteAcceptee { get; set; }
    public decimal QuantiteEndommagee { get; set; }
    public string? Notes { get; set; }
}

public class FiltreCuissonsRequete : PagedRequest
{
    public int? FourId { get; set; }
    public FiringBatchStatus? Statut { get; set; }
}

// ------------------------------------------------------------ Décoration

public record DecorationDto(
    int Id,
    string Reference,
    int TypeDecorationId,
    string TypeDecorationNom,
    int? ProductionId,
    string? ProductionNumero,
    int? CommandeId,
    string? CommandeNumero,
    decimal Quantite,
    DecorationStatus Statut,
    string StatutLibelle,
    string? Couleurs,
    string? Email,
    string? Peinture,
    decimal? QuantiteOr,
    decimal? QuantiteArgent,
    string? MateriauxUtilises,
    decimal Cout,
    int? EmployeId,
    string? EmployeNom,
    DateTime? DateDebut,
    DateTime? DateFin,
    string? Notes,
    IReadOnlyList<string> Photos);

public class DecorationRequete
{
    public int TypeDecorationId { get; set; }
    public int? ProductionId { get; set; }
    public int? CommandeId { get; set; }
    public decimal Quantite { get; set; }
    public string? Couleurs { get; set; }
    public string? Email { get; set; }
    public string? Peinture { get; set; }
    public decimal? QuantiteOr { get; set; }
    public decimal? QuantiteArgent { get; set; }
    public string? MateriauxUtilises { get; set; }
    public decimal Cout { get; set; }
    public int? EmployeId { get; set; }
    public DateTime? DateDebut { get; set; }
    public DateTime? DateFin { get; set; }
    public string? Notes { get; set; }
}

public class FiltreDecorationsRequete : PagedRequest
{
    public DecorationStatus? Statut { get; set; }
    public int? ProductionId { get; set; }
}

// -------------------------------------------------------- Contrôle qualité

public record ControleQualiteDto(
    int Id,
    string Reference,
    int? ProductionId,
    string? ProductionNumero,
    int? CommandeId,
    string? CommandeNumero,
    DateTime Date,
    string? Controleur,
    decimal QuantiteControlee,
    decimal QuantiteAcceptee,
    decimal QuantiteRefusee,
    decimal QuantiteARetoucher,
    QualityResult Resultat,
    string ResultatLibelle,
    bool FissuresConformes,
    bool FormeConforme,
    bool CouleurConforme,
    bool EmailConforme,
    bool DecorationConforme,
    bool DimensionsConformes,
    bool SurfaceConforme,
    bool CuissonConforme,
    string? Notes,
    IReadOnlyList<DefautQualiteDto> Defauts);

public record DefautQualiteDto(
    int Id,
    QualityCheckPoint PointControle,
    string PointControleLibelle,
    IssueSeverity Gravite,
    string GraviteLibelle,
    IssueResolution Solution,
    string SolutionLibelle,
    decimal Quantite,
    string Description,
    string? Remede);

public class ControleQualiteRequete
{
    public int? ProductionId { get; set; }
    public int? CommandeId { get; set; }
    public decimal QuantiteControlee { get; set; }
    public decimal QuantiteAcceptee { get; set; }
    public decimal QuantiteRefusee { get; set; }
    public decimal QuantiteARetoucher { get; set; }

    public bool FissuresConformes { get; set; } = true;
    public bool FormeConforme { get; set; } = true;
    public bool CouleurConforme { get; set; } = true;
    public bool EmailConforme { get; set; } = true;
    public bool DecorationConforme { get; set; } = true;
    public bool DimensionsConformes { get; set; } = true;
    public bool SurfaceConforme { get; set; } = true;
    public bool CuissonConforme { get; set; } = true;

    public string? Notes { get; set; }
    public List<DefautQualiteRequete> Defauts { get; set; } = new();
}

public class DefautQualiteRequete
{
    public QualityCheckPoint PointControle { get; set; }
    public IssueSeverity Gravite { get; set; } = IssueSeverity.Mineure;
    public IssueResolution Solution { get; set; } = IssueResolution.ADecider;
    public decimal Quantite { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Remede { get; set; }
}

public class FiltreControlesRequete : PagedRequest
{
    public QualityResult? Resultat { get; set; }
    public int? ProductionId { get; set; }
}
