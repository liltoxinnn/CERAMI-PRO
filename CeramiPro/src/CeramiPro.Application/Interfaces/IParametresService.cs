using CeramiPro.Application.DTOs.Settings;

namespace CeramiPro.Application.Interfaces;

public interface IParametresService
{
    Task<ParametresAtelierDto> ObtenirAsync(CancellationToken cancellationToken = default);

    Task<ParametresAtelierDto> ModifierAsync(ParametresAtelierDto requete, CancellationToken cancellationToken = default);
}
