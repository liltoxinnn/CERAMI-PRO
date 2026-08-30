using CeramicWorkshop.Domain.Entities.Identity;

namespace CeramicWorkshop.Application.Interfaces;

/// <summary>Génération des jetons d'authentification.</summary>
public interface ITokenService
{
    /// <summary>Crée le jeton d'accès de l'utilisateur avec ses droits.</summary>
    (string Jeton, DateTime Expiration) CreerJetonAcces(User utilisateur, IReadOnlyList<string> droits);

    /// <summary>Crée un jeton de renouvellement aléatoire.</summary>
    string CreerJetonRenouvellement();
}
