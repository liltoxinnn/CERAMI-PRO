using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Application.DTOs.Commercial;

public record VenteDto(
    int Id,
    string Numero,
    int? ClientId,
    string ClientNom,
    DateTime Date,
    SaleStatus Statut,
    string StatutLibelle,
    decimal SousTotal,
    decimal Remise,
    decimal Tva,
    decimal Total,
    decimal Paye,
    decimal Reste,
    decimal CoutRevient,
    decimal Benefice,
    string? Notes,
    string? Utilisateur,
    string? FactureNumero,
    IReadOnlyList<LigneVenteDto> Lignes);

public record LigneVenteDto(
    int Id,
    int ProduitId,
    string ProduitNom,
    string ProduitReference,
    int? VarianteId,
    string Description,
    decimal Quantite,
    decimal PrixUnitaire,
    decimal Remise,
    decimal Total);

public class VenteRequete
{
    public int? ClientId { get; set; }
    public DateTime? Date { get; set; }
    public decimal Remise { get; set; }
    public string? Notes { get; set; }
    public List<LigneVenteRequete> Lignes { get; set; } = new();

    /// <summary>Règlement encaissé immédiatement (facultatif).</summary>
    public decimal MontantPaye { get; set; }

    public int? ModeReglementId { get; set; }

    /// <summary>Émet la facture dans la foulée.</summary>
    public bool EmettreFacture { get; set; } = true;
}

public class LigneVenteRequete
{
    public int ProduitId { get; set; }
    public int? VarianteId { get; set; }
    public decimal Quantite { get; set; }
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; }
}

public class FiltreVentesRequete : PagedRequest
{
    public int? ClientId { get; set; }
    public SaleStatus? Statut { get; set; }
    public bool SeulementImpayees { get; set; }
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}

// ------------------------------------------------------------------ Factures

public record FactureDto(
    int Id,
    string Numero,
    int? ClientId,
    string ClientNom,
    int? VenteId,
    string? VenteNumero,
    int? CommandeId,
    string? CommandeNumero,
    DateTime DateEmission,
    DateTime? DateEcheance,
    decimal SousTotal,
    decimal Remise,
    decimal TauxTva,
    decimal Tva,
    decimal Total,
    decimal Paye,
    decimal Reste,
    InvoiceStatus Statut,
    string StatutLibelle,
    string? Notes,
    IReadOnlyList<LigneFactureDto> Lignes);

public record LigneFactureDto(
    int Id, int? ProduitId, string Description, decimal Quantite,
    decimal PrixUnitaire, decimal Remise, decimal Total);

public class FiltreFacturesRequete : PagedRequest
{
    public int? ClientId { get; set; }
    public InvoiceStatus? Statut { get; set; }
    public bool SeulementImpayees { get; set; }
}

/// <summary>Facture destinée à une commande personnalisée.</summary>
public class FactureCommandeRequete
{
    public int CommandeId { get; set; }
    public DateTime? DateEcheance { get; set; }
    public string? Notes { get; set; }
}

// ----------------------------------------------------------------- Paiements

public record PaiementDto(
    int Id,
    string Numero,
    int? ClientId,
    string? ClientNom,
    int? VenteId,
    string? VenteNumero,
    int? CommandeId,
    string? CommandeNumero,
    int? FactureId,
    string? FactureNumero,
    decimal Montant,
    DateTime Date,
    int ModeReglementId,
    string ModeReglement,
    bool Acompte,
    string? Reference,
    string? Notes,
    string? Utilisateur);

public class PaiementRequete
{
    public int? ClientId { get; set; }
    public int? VenteId { get; set; }
    public int? CommandeId { get; set; }
    public int? FactureId { get; set; }
    public decimal Montant { get; set; }
    public DateTime? Date { get; set; }
    public int ModeReglementId { get; set; }
    public bool Acompte { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}

public class FiltrePaiementsRequete : PagedRequest
{
    public int? ClientId { get; set; }
    public DateTime? Du { get; set; }
    public DateTime? Au { get; set; }
}

/// <summary>Dette d'un client, affichée dans l'écran « Dettes clients ».</summary>
public record DetteClientDto(
    int ClientId,
    string ClientNom,
    string? Telephone,
    decimal TotalDu,
    decimal TotalPaye,
    decimal Reste,
    DateTime? PlusAncienneEcheance);
