using CeramiPro.Application.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Commercial;

public record ClientDto(
    int Id,
    string Numero,
    string Nom,
    string? Telephone,
    string? Instagram,
    string? Email,
    string? Adresse,
    string? Ville,
    string? Notes,
    bool Actif,
    decimal TotalDepense,
    decimal TotalPaye,
    decimal Reste,
    int NombreVentes,
    int NombreCommandes,
    DateTime? DerniereTransaction);

public class ClientRequete
{
    public string Nom { get; set; } = string.Empty;
    public string? Telephone { get; set; }

    /// <summary>Compte Instagram, avec ou sans « @ ».</summary>
    public string? Instagram { get; set; }

    public string? Email { get; set; }
    public string? Adresse { get; set; }
    public string? Ville { get; set; }
    public string? Notes { get; set; }
    public bool Actif { get; set; } = true;
}

public class FiltreClientsRequete : PagedRequest
{
    public bool SeulementAvecDette { get; set; }
    public bool InclureInactifs { get; set; } = true;
}

/// <summary>Client proposé dans une liste de choix : nom et de quoi le reconnaître.</summary>
public record ClientChoixDto(int Id, string Nom, string? Telephone, string? Instagram)
{
    public string Libelle => string.Join(" — ",
        new[] { Nom, Telephone, Instagram }.Where(v => !string.IsNullOrWhiteSpace(v)));
}

/// <summary>Note datée ajoutée à la fiche d'un client.</summary>
public record NoteClientDto(int Id, string Contenu, string? Auteur, DateTime Date);

// -------------------------------------------------- Commandes personnalisées

public record CommandeDto(
    int Id,
    string Numero,
    int ClientId,
    string ClientNom,
    string? ClientTelephone,
    string Titre,
    CanalCommande Canal,
    string CanalLibelle,
    string? Description,
    decimal? Largeur,
    decimal? Hauteur,
    decimal? Profondeur,
    string? Couleurs,
    string? Materiaux,
    decimal Quantite,
    decimal PrixUnitaire,
    decimal Remise,
    decimal Total,
    decimal Paye,
    decimal Reste,
    DateTime DateCommande,
    DateTime DateLimite,
    DateTime? DateLivraison,
    CustomOrderStatus Statut,
    string StatutLibelle,
    int? EmployeId,
    string? EmployeNom,
    string? Notes,
    bool EnRetard,
    int JoursRestants,
    IReadOnlyList<PhotoCommandeDto> Photos,
    IReadOnlyList<NoteCommandeDto> NotesHistorique);

public record PhotoCommandeDto(
    int Id, string Chemin, string? Legende, CustomOrderImageKind Type, string TypeLibelle);

public record NoteCommandeDto(int Id, string Contenu, string? Auteur, DateTime Date);

public class CommandeRequete
{
    public int ClientId { get; set; }
    public string Titre { get; set; } = string.Empty;

    /// <summary>Par où la commande est arrivée.</summary>
    public CanalCommande Canal { get; set; } = CanalCommande.Atelier;

    public string? Description { get; set; }
    public decimal? Largeur { get; set; }
    public decimal? Hauteur { get; set; }
    public decimal? Profondeur { get; set; }
    public string? Couleurs { get; set; }
    public string? Materiaux { get; set; }
    public decimal Quantite { get; set; } = 1m;
    public decimal PrixUnitaire { get; set; }
    public decimal Remise { get; set; }
    public DateTime? DateLimite { get; set; }
    public int? EmployeId { get; set; }
    public string? Notes { get; set; }
}

public class FiltreCommandesRequete : PagedRequest
{
    public CustomOrderStatus? Statut { get; set; }
    public CanalCommande? Canal { get; set; }
    public int? ClientId { get; set; }
    public bool SeulementEnRetard { get; set; }
    public bool SeulementProchesEcheance { get; set; }
    public bool SeulementEnCours { get; set; }

    /// <summary>Commandes non annulées dont il reste une somme à encaisser.</summary>
    public bool SeulementAvecReste { get; set; }
}

public class PhotoCommandeRequete
{
    public string Chemin { get; set; } = string.Empty;
    public string? Legende { get; set; }
    public CustomOrderImageKind Type { get; set; } = CustomOrderImageKind.Reference;
}

public class NoteRequete
{
    public string Contenu { get; set; } = string.Empty;
}
