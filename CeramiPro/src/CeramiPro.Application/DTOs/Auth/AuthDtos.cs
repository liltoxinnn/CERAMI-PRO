using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Auth;

/// <summary>Identifiants saisis dans l'écran de connexion.</summary>
public class ConnexionRequete
{
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
}

/// <summary>
/// Profil renvoyé après une connexion réussie. Une application de bureau
/// n'a pas de jeton : la session vit dans le programme, jusqu'à sa fermeture.
/// </summary>
public record ConnexionReponse(UtilisateurConnecteDto Utilisateur);

/// <summary>Profil de l'utilisateur connecté.</summary>
public record UtilisateurConnecteDto(
    int Id,
    string NomUtilisateur,
    string NomComplet,
    string? Email,
    RoleUtilisateur Role,
    string RoleNom,
    bool DoitChangerMotDePasse);

/// <summary>Changement de mot de passe par l'utilisateur lui-même.</summary>
public class ChangementMotDePasseRequete
{
    public string MotDePasseActuel { get; set; } = string.Empty;
    public string NouveauMotDePasse { get; set; } = string.Empty;
    public string ConfirmationMotDePasse { get; set; } = string.Empty;
}
