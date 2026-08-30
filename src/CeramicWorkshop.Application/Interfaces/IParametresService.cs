using CeramicWorkshop.Application.DTOs.Settings;

namespace CeramicWorkshop.Application.Interfaces;

public interface IParametresService
{
    Task<ParametresAtelierDto> ObtenirAsync(CancellationToken cancellationToken = default);

    Task<ParametresAtelierDto> ModifierAsync(ParametresAtelierDto requete, CancellationToken cancellationToken = default);
}
