using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Production;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Ordres de production et suivi des étapes de fabrication.</summary>
[ApiController]
[Route("api/production")]
public class ProductionController : ControllerBase
{
    private readonly IProductionService _production;

    public ProductionController(IProductionService production) => _production = production;

    /// <summary>Liste paginée des ordres de production.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.ProductionConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreProductionsRequete requete, CancellationToken cancellationToken)
        => Ok(await _production.ListerAsync(requete, cancellationToken));

    /// <summary>Tableau de production : les ordres regroupés par étape.</summary>
    [HttpGet("tableau")]
    [DroitRequis(PermissionCodes.ProductionConsulter)]
    public async Task<IActionResult> Tableau(CancellationToken cancellationToken)
        => Ok(await _production.TableauAsync(cancellationToken));

    /// <summary>Chiffres clés de la production en cours.</summary>
    [HttpGet("synthese")]
    [DroitRequis(PermissionCodes.ProductionConsulter)]
    public async Task<IActionResult> Synthese(CancellationToken cancellationToken)
        => Ok(await _production.SyntheseAsync(cancellationToken));

    /// <summary>Détail d'un ordre de production.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.ProductionConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _production.ObtenirAsync(id, cancellationToken));

    /// <summary>Crée un ordre de production.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.ProductionGerer)]
    public async Task<IActionResult> Creer(OrdreProductionRequete requete, CancellationToken cancellationToken)
    {
        var ordre = await _production.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = ordre.Id }, ordre);
    }

    /// <summary>Modifie un ordre encore planifié.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.ProductionGerer)]
    public async Task<IActionResult> Modifier(
        int id, OrdreProductionRequete requete, CancellationToken cancellationToken)
        => Ok(await _production.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Lance la production : vérifie puis consomme les matières.</summary>
    [HttpPost("{id:int}/lancement")]
    [DroitRequis(PermissionCodes.ProductionGerer)]
    public async Task<IActionResult> Lancer(
        int id, LancementProductionRequete requete, CancellationToken cancellationToken)
        => Ok(await _production.LancerAsync(id, requete, cancellationToken));

    /// <summary>Fait avancer la production à l'étape suivante.</summary>
    [HttpPost("{id:int}/etape")]
    [DroitRequis(PermissionCodes.ProductionChangerEtape)]
    public async Task<IActionResult> ChangerEtape(
        int id, ChangementEtapeRequete requete, CancellationToken cancellationToken)
        => Ok(await _production.ChangerEtapeAsync(id, requete, cancellationToken));

    /// <summary>Annule la production et remet les matières en stock.</summary>
    [HttpPost("{id:int}/annulation")]
    [DroitRequis(PermissionCodes.ProductionGerer)]
    public async Task<IActionResult> Annuler(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
        => Ok(await _production.AnnulerAsync(id, requete.Motif, cancellationToken));
}
