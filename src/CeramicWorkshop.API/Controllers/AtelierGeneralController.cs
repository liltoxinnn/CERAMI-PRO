using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Alertes;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Recherche globale dans toutes les fiches de l'atelier.</summary>
[ApiController]
[Route("api/recherche")]
public class RechercheController : ControllerBase
{
    private readonly IRechercheService _recherche;

    public RechercheController(IRechercheService recherche) => _recherche = recherche;

    /// <summary>
    /// Cherche un produit, une matière, un client, une commande… La recherche
    /// tolère les fautes de frappe et les accents manquants, et ne parcourt que
    /// les modules que l'utilisateur a le droit de consulter.
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Chercher(
        [FromQuery] string terme, [FromQuery] int parFamille = 5, CancellationToken cancellationToken = default)
        => Ok(await _recherche.ChercherAsync(terme, parFamille, cancellationToken));
}

/// <summary>Centre d'alertes de l'atelier.</summary>
[ApiController]
[Route("api/alertes")]
public class AlertesController : ControllerBase
{
    private readonly IAlerteService _alertes;

    public AlertesController(IAlerteService alertes) => _alertes = alertes;

    /// <summary>Alertes ouvertes, recalculées à partir de l'état réel de l'atelier.</summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreAlertesRequete requete, CancellationToken cancellationToken)
        => Ok(await _alertes.ListerAsync(requete, cancellationToken));

    /// <summary>Compteurs affichés dans l'en-tête.</summary>
    [HttpGet("resume")]
    [Authorize]
    public async Task<IActionResult> Resume(CancellationToken cancellationToken)
        => Ok(await _alertes.ResumeAsync(cancellationToken));

    /// <summary>Marque une alerte comme lue.</summary>
    [HttpPost("{id:int}/lue")]
    [Authorize]
    public async Task<IActionResult> MarquerLue(int id, CancellationToken cancellationToken)
    {
        await _alertes.MarquerLueAsync(id, cancellationToken);
        return Ok(new { message = "Alerte marquée comme lue." });
    }

    /// <summary>Marque toutes les alertes comme lues.</summary>
    [HttpPost("tout-lu")]
    [Authorize]
    public async Task<IActionResult> ToutMarquerLu(CancellationToken cancellationToken)
    {
        await _alertes.ToutMarquerLuAsync(cancellationToken);
        return Ok(new { message = "Toutes les alertes sont marquées comme lues." });
    }

    /// <summary>Réglages des alertes.</summary>
    [HttpGet("reglages")]
    [DroitRequis(PermissionCodes.ParametresConsulter)]
    public async Task<IActionResult> Reglages(CancellationToken cancellationToken)
        => Ok(await _alertes.ListerReglagesAsync(cancellationToken));

    /// <summary>Modifie le réglage d'une alerte.</summary>
    [HttpPut("reglages/{id:int}")]
    [DroitRequis(PermissionCodes.ParametresModifier)]
    public async Task<IActionResult> ModifierReglage(
        int id, ReglageAlerteDto reglage, CancellationToken cancellationToken)
        => Ok(await _alertes.ModifierReglageAsync(id, reglage, cancellationToken));
}

/// <summary>Sauvegardes des données de l'atelier.</summary>
[ApiController]
[Route("api/sauvegardes")]
[DroitRequis(PermissionCodes.SauvegardeGerer)]
public class SauvegardesController : ControllerBase
{
    private readonly ISauvegardeService _sauvegardes;

    public SauvegardesController(ISauvegardeService sauvegardes) => _sauvegardes = sauvegardes;

    /// <summary>État du dispositif et liste des sauvegardes disponibles.</summary>
    [HttpGet]
    public async Task<IActionResult> Etat(CancellationToken cancellationToken)
        => Ok(await _sauvegardes.EtatAsync(cancellationToken));

    /// <summary>Crée immédiatement une sauvegarde.</summary>
    [HttpPost]
    public async Task<IActionResult> Creer(CancellationToken cancellationToken)
        => Ok(await _sauvegardes.CreerAsync(false, cancellationToken));

    /// <summary>Télécharge une sauvegarde existante.</summary>
    [HttpGet("{nomFichier}")]
    public async Task<IActionResult> Telecharger(string nomFichier, CancellationToken cancellationToken)
    {
        var (nom, contenu) = await _sauvegardes.TelechargerAsync(nomFichier, cancellationToken);
        return File(contenu, "application/zip", nom);
    }

    /// <summary>Supprime une sauvegarde.</summary>
    [HttpDelete("{nomFichier}")]
    public async Task<IActionResult> Supprimer(string nomFichier, CancellationToken cancellationToken)
    {
        await _sauvegardes.SupprimerAsync(nomFichier, cancellationToken);
        return Ok(new { message = "Sauvegarde supprimée." });
    }
}
