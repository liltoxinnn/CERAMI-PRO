using CeramiPro.Application.Common;

namespace CeramiPro.Application.DTOs.Stock;

public record FournisseurDto(
    int Id,
    string Numero,
    string Nom,
    string? Entreprise,
    string? Telephone,
    string? Email,
    string? Adresse,
    string? Ville,
    string? Notes,
    bool Actif,
    decimal TotalAchats,
    decimal TotalPaye,
    decimal Reste,
    int NombreMatieres,
    DateTime? DernierAchat);

public class FournisseurRequete
{
    public string Nom { get; set; } = string.Empty;
    public string? Entreprise { get; set; }
    public string? Telephone { get; set; }
    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Notes { get; set; }
    public bool Actif { get; set; } = true;
}

public class FiltreFournisseursRequete : PagedRequest
{
    public bool SeulementAvecDette { get; set; }
    public bool InclureInactifs { get; set; } = true;
}

/// <summary>Règlement versé à un fournisseur.</summary>
public record ReglementFournisseurDto(
    int Id,
    string Numero,
    int FournisseurId,
    string FournisseurNom,
    int? AchatId,
    string? AchatNumero,
    decimal Montant,
    DateTime Date,
    int ModeReglementId,
    string ModeReglement,
    string? Reference,
    string? Notes,
    string? Utilisateur);

public class ReglementFournisseurRequete
{
    public int FournisseurId { get; set; }
    public int? AchatId { get; set; }
    public decimal Montant { get; set; }
    public DateTime? Date { get; set; }
    public int ModeReglementId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }
}
