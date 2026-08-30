using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Settings;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Paramètres de l'atelier.</summary>
[ApiController]
[Route("api/parametres")]
public class ParametresController : ControllerBase
{
    private readonly IParametresService _parametres;

    public ParametresController(IParametresService parametres) => _parametres = parametres;

    /// <summary>Paramètres actuels de l'atelier.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.ParametresConsulter)]
    [ProducesResponseType(typeof(ParametresAtelierDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Obtenir(CancellationToken cancellationToken)
        => Ok(await _parametres.ObtenirAsync(cancellationToken));

    /// <summary>Enregistre les paramètres de l'atelier.</summary>
    [HttpPut]
    [DroitRequis(PermissionCodes.ParametresModifier)]
    [ProducesResponseType(typeof(ParametresAtelierDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Modifier(ParametresAtelierDto requete, CancellationToken cancellationToken)
        => Ok(await _parametres.ModifierAsync(requete, cancellationToken));
}
