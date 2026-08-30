using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Referentiels;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Listes simples : catégories de matières, de produits, de dépenses, types de décoration.</summary>
[ApiController]
[Route("api/referentiels/{type}")]
[Authorize]
public class ReferentielsController : ControllerBase
{
    private readonly IReferentielService _referentiels;

    public ReferentielsController(IReferentielService referentiels) => _referentiels = referentiels;

    /// <summary>Liste les éléments d'un référentiel.</summary>
    [HttpGet]
    public async Task<IActionResult> Lister(
        TypeReferentiel type, [FromQuery] bool inclureInactifs = true, CancellationToken cancellationToken = default)
        => Ok(await _referentiels.ListerAsync(type, inclureInactifs, cancellationToken));

    /// <summary>Ajoute un élément.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Creer(
        TypeReferentiel type, ElementReferentielRequete requete, CancellationToken cancellationToken)
        => Ok(await _referentiels.CreerAsync(type, requete, cancellationToken));

    /// <summary>Modifie un élément.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Modifier(
        TypeReferentiel type, int id, ElementReferentielRequete requete, CancellationToken cancellationToken)
        => Ok(await _referentiels.ModifierAsync(type, id, requete, cancellationToken));

    /// <summary>Supprime un élément non utilisé.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Supprimer(TypeReferentiel type, int id, CancellationToken cancellationToken)
    {
        await _referentiels.SupprimerAsync(type, id, cancellationToken);
        return Ok(new { message = "Élément supprimé." });
    }
}

/// <summary>Unités de mesure.</summary>
[ApiController]
[Route("api/unites")]
[Authorize]
public class UnitesController : ControllerBase
{
    private readonly IUniteService _unites;

    public UnitesController(IUniteService unites) => _unites = unites;

    /// <summary>Liste des unités de mesure.</summary>
    [HttpGet]
    public async Task<IActionResult> Lister(
        [FromQuery] bool inclureInactives = true, CancellationToken cancellationToken = default)
        => Ok(await _unites.ListerAsync(inclureInactives, cancellationToken));

    /// <summary>Crée une unité personnalisée.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Creer(UniteRequete requete, CancellationToken cancellationToken)
        => Ok(await _unites.CreerAsync(requete, cancellationToken));

    /// <summary>Modifie une unité.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Modifier(int id, UniteRequete requete, CancellationToken cancellationToken)
        => Ok(await _unites.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime une unité non utilisée.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _unites.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Unité supprimée." });
    }
}

/// <summary>Modes de règlement.</summary>
[ApiController]
[Route("api/modes-reglement")]
[Authorize]
public class ModesReglementController : ControllerBase
{
    private readonly IReferentielService _referentiels;

    public ModesReglementController(IReferentielService referentiels) => _referentiels = referentiels;

    /// <summary>Liste des modes de règlement actifs.</summary>
    [HttpGet]
    public async Task<IActionResult> Lister(CancellationToken cancellationToken)
        => Ok(await _referentiels.ListerModesReglementAsync(cancellationToken));
}
