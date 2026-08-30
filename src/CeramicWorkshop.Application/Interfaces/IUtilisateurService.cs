using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Identity;

namespace CeramicWorkshop.Application.Interfaces;

public interface IUtilisateurService
{
    Task<PagedResult<UtilisateurDto>> ListerAsync(PagedRequest requete, CancellationToken cancellationToken = default);

    Task<UtilisateurDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<UtilisateurDto> CreerAsync(CreerUtilisateurRequete requete, CancellationToken cancellationToken = default);

    Task<UtilisateurDto> ModifierAsync(int id, ModifierUtilisateurRequete requete, CancellationToken cancellationToken = default);

    Task ReinitialiserMotDePasseAsync(int id, ReinitialiserMotDePasseRequete requete, CancellationToken cancellationToken = default);

    Task ChangerActivationAsync(int id, bool actif, CancellationToken cancellationToken = default);
}
