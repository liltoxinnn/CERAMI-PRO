using CeramiPro.Application.DTOs.Auth;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Ouverture et fermeture de session. Une application de bureau n'a qu'un
/// utilisateur à la fois : il n'y a ni jeton ni renouvellement.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Vérifie les identifiants et ouvre la session. Lève une
    /// <see cref="Common.RegleMetierException"/> avec un message destiné à
    /// l'utilisateur si la connexion est refusée.
    /// </summary>
    Task<ConnexionReponse> ConnecterAsync(
        ConnexionRequete requete, CancellationToken cancellationToken = default);

    Task DeconnecterAsync(CancellationToken cancellationToken = default);

    /// <summary>Changement de mot de passe par la personne elle-même.</summary>
    Task ChangerMotDePasseAsync(
        ChangementMotDePasseRequete requete, CancellationToken cancellationToken = default);
}
