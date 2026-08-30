namespace CeramicWorkshop.Application.DTOs.Auth;

/// <summary>Identifiants saisis dans l'écran de connexion.</summary>
public class ConnexionRequete
{
    public string NomUtilisateur { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
}

/// <summary>Jetons et profil renvoyés après une connexion réussie.</summary>
public record ConnexionReponse(
    string JetonAcces,
    DateTime ExpirationJeton,
    string JetonRenouvellement,
    UtilisateurConnecteDto Utilisateur);

/// <summary>Profil de l'utilisateur connecté, avec la liste de ses droits.</summary>
public record UtilisateurConnecteDto(
    int Id,
    string NomUtilisateur,
    string NomComplet,
    string? Email,
    string RoleCode,
    string RoleNom,
    IReadOnlyList<string> Droits,
    bool DoitChangerMotDePasse);

/// <summary>Demande de renouvellement du jeton d'accès.</summary>
public class RenouvellementRequete
{
    public string JetonRenouvellement { get; set; } = string.Empty;
}

/// <summary>Changement de mot de passe par l'utilisateur lui-même.</summary>
public class ChangementMotDePasseRequete
{
    public string MotDePasseActuel { get; set; } = string.Empty;
    public string NouveauMotDePasse { get; set; } = string.Empty;
    public string ConfirmationMotDePasse { get; set; } = string.Empty;
}
