namespace CeramicWorkshop.Application.DTOs.Identity;

/// <summary>Utilisateur affiché dans la liste et la fiche.</summary>
public record UtilisateurDto(
    int Id,
    string NomUtilisateur,
    string NomComplet,
    string? Email,
    string? Telephone,
    int RoleId,
    string RoleNom,
    bool Actif,
    bool DoitChangerMotDePasse,
    DateTime? DerniereConnexion,
    DateTime DateCreation);

public class CreerUtilisateurRequete
{
    public string NomUtilisateur { get; set; } = string.Empty;
    public string NomComplet { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public string MotDePasse { get; set; } = string.Empty;
    public int RoleId { get; set; }
    public bool Actif { get; set; } = true;
    public bool DoitChangerMotDePasse { get; set; } = true;
}

public class ModifierUtilisateurRequete
{
    public string NomComplet { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Telephone { get; set; }
    public int RoleId { get; set; }
    public bool Actif { get; set; } = true;
}

public class ReinitialiserMotDePasseRequete
{
    public string NouveauMotDePasse { get; set; } = string.Empty;
    public bool DoitChangerMotDePasse { get; set; } = true;
}
