using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Rôles et droits d'accès.</summary>
[ApiController]
[Route("api/roles")]
public class RolesController : ControllerBase
{
    private readonly IRoleService _roles;

    public RolesController(IRoleService roles) => _roles = roles;

    /// <summary>Liste des rôles avec leurs droits.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.UtilisateursConsulter)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lister(CancellationToken cancellationToken)
        => Ok(await _roles.ListerAsync(cancellationToken));

    /// <summary>Fiche d'un rôle.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.UtilisateursConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _roles.ObtenirAsync(id, cancellationToken));

    /// <summary>Catalogue des droits, regroupés par module.</summary>
    [HttpGet("droits")]
    [DroitRequis(PermissionCodes.UtilisateursConsulter)]
    [ProducesResponseType(typeof(IReadOnlyList<ModuleDroitsDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListerDroits(CancellationToken cancellationToken)
        => Ok(await _roles.ListerDroitsParModuleAsync(cancellationToken));

    /// <summary>Met à jour les droits accordés à un rôle.</summary>
    [HttpPut("{id:int}/droits")]
    [DroitRequis(PermissionCodes.UtilisateursGerer)]
    public async Task<IActionResult> ModifierDroits(
        int id, ModifierDroitsRoleRequete requete, CancellationToken cancellationToken)
        => Ok(await _roles.ModifierDroitsAsync(id, requete, cancellationToken));
}
