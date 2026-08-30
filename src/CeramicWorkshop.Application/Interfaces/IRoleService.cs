using CeramicWorkshop.Application.DTOs.Identity;

namespace CeramicWorkshop.Application.Interfaces;

public interface IRoleService
{
    Task<IReadOnlyList<RoleDto>> ListerAsync(CancellationToken cancellationToken = default);

    Task<RoleDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ModuleDroitsDto>> ListerDroitsParModuleAsync(CancellationToken cancellationToken = default);

    Task<RoleDto> ModifierDroitsAsync(int id, ModifierDroitsRoleRequete requete, CancellationToken cancellationToken = default);
}
