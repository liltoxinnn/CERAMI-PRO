using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Stock;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Matières premières et consommables de l'atelier.</summary>
[ApiController]
[Route("api/matieres")]
public class MatieresController : ControllerBase
{
    private readonly IMatiereService _matieres;

    public MatieresController(IMatiereService matieres) => _matieres = matieres;

    /// <summary>Liste paginée des matières premières.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.MatieresConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreMatieresRequete requete, CancellationToken cancellationToken)
        => Ok(await _matieres.ListerAsync(requete, cancellationToken));

    /// <summary>Synthèse : nombre d'articles, alertes et valeur du stock.</summary>
    [HttpGet("synthese")]
    [DroitRequis(PermissionCodes.MatieresConsulter)]
    public async Task<IActionResult> Synthese(CancellationToken cancellationToken)
        => Ok(await _matieres.SyntheseAsync(cancellationToken));

    /// <summary>Matières dont le stock est passé sous le seuil minimum.</summary>
    [HttpGet("stock-faible")]
    [DroitRequis(PermissionCodes.MatieresConsulter)]
    public async Task<IActionResult> StockFaible(CancellationToken cancellationToken)
        => Ok(await _matieres.ListerStockFaibleAsync(cancellationToken));

    /// <summary>Fiche d'une matière première.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _matieres.ObtenirAsync(id, cancellationToken));

    /// <summary>Lots reçus pour une matière.</summary>
    [HttpGet("{id:int}/lots")]
    [DroitRequis(PermissionCodes.MatieresConsulter)]
    public async Task<IActionResult> Lots(int id, CancellationToken cancellationToken)
        => Ok(await _matieres.ListerLotsAsync(id, cancellationToken));

    /// <summary>Crée une matière première.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Creer(MatiereRequete requete, CancellationToken cancellationToken)
    {
        var matiere = await _matieres.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = matiere.Id }, matiere);
    }

    /// <summary>Modifie une matière première.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Modifier(int id, MatiereRequete requete, CancellationToken cancellationToken)
        => Ok(await _matieres.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime une matière sans historique.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.MatieresGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _matieres.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Matière supprimée." });
    }
}

/// <summary>Mouvements de stock.</summary>
[ApiController]
[Route("api/mouvements")]
public class MouvementsController : ControllerBase
{
    private readonly IInventaireService _inventaire;

    public MouvementsController(IInventaireService inventaire) => _inventaire = inventaire;

    /// <summary>Historique des mouvements de stock.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.MouvementsConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreMouvementsRequete requete, CancellationToken cancellationToken)
        => Ok(await _inventaire.ListerAsync(requete, cancellationToken));

    /// <summary>Enregistre une régularisation après comptage physique.</summary>
    [HttpPost("regularisation")]
    [DroitRequis(PermissionCodes.MouvementsGerer)]
    public async Task<IActionResult> Regulariser(
        RegularisationRequete requete, CancellationToken cancellationToken)
        => Ok(await _inventaire.RegulariserAsync(requete, cancellationToken));
}
