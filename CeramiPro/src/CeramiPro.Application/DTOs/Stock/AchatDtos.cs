using CeramiPro.Application.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Stock;

public record AchatDto(
    int Id,
    string Numero,
    int FournisseurId,
    string FournisseurNom,
    DateTime Date,
    PurchaseStatus Statut,
    string StatutLibelle,
    decimal SousTotal,
    decimal Remise,
    decimal FraisLivraison,
    decimal Total,
    decimal Paye,
    decimal Reste,
    string? ReferenceFacture,
    string? Notes,
    string? Utilisateur,
    IReadOnlyList<LigneAchatDto> Lignes);

public record LigneAchatDto(
    int Id,
    int MatiereId,
    string MatiereNom,
    string MatiereReference,
    int UniteId,
    string UniteCode,
    decimal Quantite,
    decimal QuantiteRecue,
    decimal PrixUnitaire,
    decimal Remise,
    decimal Total,
    string? Notes);

public class AchatRequete
{
    public int FournisseurId { get; set; }
    public DateTime? Date { get; set; }
    public decimal Remise { get; set; }
    public decimal FraisLivraison { get; set; }
    public string? ReferenceFacture { get; set; }
    public string? Notes { get; set; }
    public List<LigneAchatRequete> Lignes { get; set; } = new();
}

public class LigneAchatRequete
{
    public int MatiereId { get; set; }
    public int UniteId { get; set; }
    public decimal Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; }
    public string? Notes { get; set; }
}

/// <summary>Réception d'un achat : les quantités reçues entrent en stock.</summary>
public class ReceptionAchatRequete
{
    public List<LigneReceptionRequete> Lignes { get; set; } = new();
    public string? Notes { get; set; }
}

public class LigneReceptionRequete
{
    public int LigneAchatId { get; set; }
    public decimal QuantiteRecue { get; set; }
    public DateTime? DatePeremption { get; set; }
    public string? Emplacement { get; set; }
}

public class FiltreAchatsRequete : PagedRequest
{
    public int? FournisseurId { get; set; }
    public PurchaseStatus? Statut { get; set; }
    public bool SeulementImpayes { get; set; }
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}
