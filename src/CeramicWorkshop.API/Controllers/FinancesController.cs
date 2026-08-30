using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Finances;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Dépenses de l'atelier.</summary>
[ApiController]
[Route("api/depenses")]
public class DepensesController : ControllerBase
{
    private readonly IDepenseService _depenses;

    public DepensesController(IDepenseService depenses) => _depenses = depenses;

    /// <summary>Liste paginée des dépenses.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.DepensesConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreDepensesRequete requete, CancellationToken cancellationToken)
        => Ok(await _depenses.ListerAsync(requete, cancellationToken));

    /// <summary>Enregistre une dépense.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.DepensesGerer)]
    public async Task<IActionResult> Creer(DepenseRequete requete, CancellationToken cancellationToken)
        => Ok(await _depenses.CreerAsync(requete, cancellationToken));

    /// <summary>Modifie une dépense.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.DepensesGerer)]
    public async Task<IActionResult> Modifier(int id, DepenseRequete requete, CancellationToken cancellationToken)
        => Ok(await _depenses.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime une dépense en conservant sa trace.</summary>
    [HttpPost("{id:int}/suppression")]
    [DroitRequis(PermissionCodes.DepensesGerer)]
    public async Task<IActionResult> Supprimer(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
    {
        await _depenses.SupprimerAsync(id, requete.Motif, cancellationToken);
        return Ok(new { message = "Dépense supprimée." });
    }
}

/// <summary>Tableau de bord de l'atelier.</summary>
[ApiController]
[Route("api/tableau-de-bord")]
public class TableauDeBordController : ControllerBase
{
    private readonly ITableauDeBordService _tableau;

    public TableauDeBordController(ITableauDeBordService tableau) => _tableau = tableau;

    /// <summary>Chiffres clés et graphiques du tableau de bord.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.TableauDeBordConsulter)]
    public async Task<IActionResult> Obtenir(CancellationToken cancellationToken)
        => Ok(await _tableau.ObtenirAsync(cancellationToken));
}

/// <summary>Rapports de gestion.</summary>
[ApiController]
[Route("api/rapports")]
public class RapportsController : ControllerBase
{
    private readonly IRapportService _rapports;

    public RapportsController(IRapportService rapports) => _rapports = rapports;

    /// <summary>Génère un rapport sur la période demandée.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.RapportsConsulter)]
    public async Task<IActionResult> Generer(
        [FromQuery] RapportRequete requete, CancellationToken cancellationToken)
        => Ok(await _rapports.GenererAsync(requete, cancellationToken));

    /// <summary>Exporte le rapport au format tableur (CSV lisible par Excel).</summary>
    [HttpGet("export")]
    [DroitRequis(PermissionCodes.RapportsExporter)]
    public async Task<IActionResult> Exporter(
        [FromQuery] RapportRequete requete, CancellationToken cancellationToken)
    {
        var (nom, contenu) = await _rapports.ExporterCsvAsync(requete, cancellationToken);
        return File(contenu, "text/csv; charset=utf-8", nom);
    }
}

/// <summary>Calculateurs d'aide à la préparation d'une production.</summary>
[ApiController]
[Route("api/calculateurs")]
public class CalculateursController : ControllerBase
{
    private readonly ICalculateurService _calculateurs;

    public CalculateursController(ICalculateurService calculateurs) => _calculateurs = calculateurs;

    /// <summary>Surface à couvrir, perte comprise.</summary>
    [HttpPost("surface")]
    public IActionResult Surface(CalculSurfaceRequete requete) => Ok(_calculateurs.Surface(requete));

    /// <summary>Nombre d'unités à prévoir, perte comprise.</summary>
    [HttpPost("quantite")]
    public IActionResult Quantite(CalculQuantiteRequete requete) => Ok(_calculateurs.Quantite(requete));
}
