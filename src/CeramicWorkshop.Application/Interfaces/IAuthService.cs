using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Auth;

namespace CeramicWorkshop.Application.Interfaces;

public interface IAuthService
{
    Task<Result<ConnexionReponse>> ConnexionAsync(ConnexionRequete requete, CancellationToken cancellationToken = default);

    Task<Result<ConnexionReponse>> RenouvelerAsync(RenouvellementRequete requete, CancellationToken cancellationToken = default);

    Task<Result> DeconnexionAsync(CancellationToken cancellationToken = default);

    Task<Result> ChangerMotDePasseAsync(ChangementMotDePasseRequete requete, CancellationToken cancellationToken = default);

    Task<UtilisateurConnecteDto?> ObtenirProfilAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> ObtenirDroitsDuRoleAsync(int roleId, CancellationToken cancellationToken = default);
}
