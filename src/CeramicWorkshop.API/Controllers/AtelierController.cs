using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Production;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Fours de l'atelier.</summary>
[ApiController]
[Route("api/fours")]
public class FoursController : ControllerBase
{
    private readonly IFourService _fours;

    public FoursController(IFourService fours) => _fours = fours;

    /// <summary>Liste des fours.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.CuissonConsulter)]
    public async Task<IActionResult> Lister(CancellationToken cancellationToken)
        => Ok(await _fours.ListerAsync(cancellationToken));

    /// <summary>Ajoute un four.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Creer(FourRequete requete, CancellationToken cancellationToken)
        => Ok(await _fours.CreerAsync(requete, cancellationToken));

    /// <summary>Modifie un four.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Modifier(int id, FourRequete requete, CancellationToken cancellationToken)
        => Ok(await _fours.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime un four jamais utilisé.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _fours.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Four supprimé." });
    }
}

/// <summary>Lots de cuisson.</summary>
[ApiController]
[Route("api/cuissons")]
public class CuissonsController : ControllerBase
{
    private readonly ICuissonService _cuissons;

    public CuissonsController(ICuissonService cuissons) => _cuissons = cuissons;

    /// <summary>Liste paginée des cuissons.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.CuissonConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreCuissonsRequete requete, CancellationToken cancellationToken)
        => Ok(await _cuissons.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'une cuisson.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.CuissonConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _cuissons.ObtenirAsync(id, cancellationToken));

    /// <summary>Prépare une fournée.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Creer(CuissonRequete requete, CancellationToken cancellationToken)
    {
        var cuisson = await _cuissons.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = cuisson.Id }, cuisson);
    }

    /// <summary>Démarre la cuisson.</summary>
    [HttpPost("{id:int}/demarrage")]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Demarrer(int id, CancellationToken cancellationToken)
        => Ok(await _cuissons.DemarrerAsync(id, cancellationToken));

    /// <summary>Défourne et enregistre le résultat.</summary>
    [HttpPost("{id:int}/defournement")]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Defourner(
        int id, DefournementRequete requete, CancellationToken cancellationToken)
        => Ok(await _cuissons.DefournerAsync(id, requete, cancellationToken));

    /// <summary>Annule une cuisson non terminée.</summary>
    [HttpPost("{id:int}/annulation")]
    [DroitRequis(PermissionCodes.CuissonGerer)]
    public async Task<IActionResult> Annuler(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
        => Ok(await _cuissons.AnnulerAsync(id, requete.Motif, cancellationToken));
}

/// <summary>Travaux de décoration.</summary>
[ApiController]
[Route("api/decorations")]
public class DecorationsController : ControllerBase
{
    private readonly IDecorationService _decorations;

    public DecorationsController(IDecorationService decorations) => _decorations = decorations;

    /// <summary>Liste paginée des travaux de décoration.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.DecorationConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreDecorationsRequete requete, CancellationToken cancellationToken)
        => Ok(await _decorations.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'un travail de décoration.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.DecorationConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _decorations.ObtenirAsync(id, cancellationToken));

    /// <summary>Crée un travail de décoration.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.DecorationGerer)]
    public async Task<IActionResult> Creer(DecorationRequete requete, CancellationToken cancellationToken)
    {
        var decoration = await _decorations.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = decoration.Id }, decoration);
    }

    /// <summary>Modifie un travail de décoration.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.DecorationGerer)]
    public async Task<IActionResult> Modifier(
        int id, DecorationRequete requete, CancellationToken cancellationToken)
        => Ok(await _decorations.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Change l'état d'un travail de décoration.</summary>
    [HttpPost("{id:int}/statut")]
    [DroitRequis(PermissionCodes.DecorationGerer)]
    public async Task<IActionResult> ChangerStatut(
        int id, [FromBody] StatutDecorationRequete requete, CancellationToken cancellationToken)
        => Ok(await _decorations.ChangerStatutAsync(id, requete.Statut, cancellationToken));

    /// <summary>Ajoute une photo du décor.</summary>
    [HttpPost("{id:int}/photos")]
    [DroitRequis(PermissionCodes.DecorationGerer)]
    public async Task<IActionResult> AjouterPhoto(
        int id, [FromBody] PhotoRequete requete, CancellationToken cancellationToken)
        => Ok(await _decorations.AjouterPhotoAsync(id, requete.Chemin, requete.Legende, cancellationToken));
}

/// <summary>Contrôles qualité.</summary>
[ApiController]
[Route("api/qualite")]
public class QualiteController : ControllerBase
{
    private readonly IQualiteService _qualite;

    public QualiteController(IQualiteService qualite) => _qualite = qualite;

    /// <summary>Liste paginée des contrôles qualité.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.QualiteConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreControlesRequete requete, CancellationToken cancellationToken)
        => Ok(await _qualite.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'un contrôle qualité.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.QualiteConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _qualite.ObtenirAsync(id, cancellationToken));

    /// <summary>Enregistre un contrôle qualité.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.QualiteControler)]
    public async Task<IActionResult> Enregistrer(
        ControleQualiteRequete requete, CancellationToken cancellationToken)
    {
        var controle = await _qualite.EnregistrerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = controle.Id }, controle);
    }
}

/// <summary>Nouvel état d'un travail de décoration.</summary>
public class StatutDecorationRequete
{
    public DecorationStatus Statut { get; set; }
}

/// <summary>Photo à rattacher à un enregistrement.</summary>
public class PhotoRequete
{
    public string Chemin { get; set; } = string.Empty;
    public string? Legende { get; set; }
}
