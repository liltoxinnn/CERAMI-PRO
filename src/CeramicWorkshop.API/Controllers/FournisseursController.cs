using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Stock;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Fournisseurs de matières premières.</summary>
[ApiController]
[Route("api/fournisseurs")]
public class FournisseursController : ControllerBase
{
    private readonly IFournisseurService _fournisseurs;

    public FournisseursController(IFournisseurService fournisseurs) => _fournisseurs = fournisseurs;

    /// <summary>Liste paginée des fournisseurs, avec leur solde.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.FournisseursConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreFournisseursRequete requete, CancellationToken cancellationToken)
        => Ok(await _fournisseurs.ListerAsync(requete, cancellationToken));

    /// <summary>Fiche d'un fournisseur.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.FournisseursConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _fournisseurs.ObtenirAsync(id, cancellationToken));

    /// <summary>Historique des règlements versés à un fournisseur.</summary>
    [HttpGet("{id:int}/reglements")]
    [DroitRequis(PermissionCodes.FournisseursConsulter)]
    public async Task<IActionResult> Reglements(int id, CancellationToken cancellationToken)
        => Ok(await _fournisseurs.ListerReglementsAsync(id, cancellationToken));

    /// <summary>Crée un fournisseur.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.FournisseursGerer)]
    public async Task<IActionResult> Creer(FournisseurRequete requete, CancellationToken cancellationToken)
    {
        var fournisseur = await _fournisseurs.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = fournisseur.Id }, fournisseur);
    }

    /// <summary>Modifie un fournisseur.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.FournisseursGerer)]
    public async Task<IActionResult> Modifier(
        int id, FournisseurRequete requete, CancellationToken cancellationToken)
        => Ok(await _fournisseurs.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime un fournisseur sans historique.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.FournisseursGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _fournisseurs.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Fournisseur supprimé." });
    }

    /// <summary>Enregistre un règlement versé à un fournisseur.</summary>
    [HttpPost("reglements")]
    [DroitRequis(PermissionCodes.PaiementsEnregistrer)]
    public async Task<IActionResult> EnregistrerReglement(
        ReglementFournisseurRequete requete, CancellationToken cancellationToken)
        => Ok(await _fournisseurs.EnregistrerReglementAsync(requete, cancellationToken));
}

/// <summary>Achats de matières premières.</summary>
[ApiController]
[Route("api/achats")]
public class AchatsController : ControllerBase
{
    private readonly IAchatService _achats;

    public AchatsController(IAchatService achats) => _achats = achats;

    /// <summary>Liste paginée des achats.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.AchatsConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreAchatsRequete requete, CancellationToken cancellationToken)
        => Ok(await _achats.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'un achat.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.AchatsConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _achats.ObtenirAsync(id, cancellationToken));

    /// <summary>Crée un achat en brouillon.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.AchatsGerer)]
    public async Task<IActionResult> Creer(AchatRequete requete, CancellationToken cancellationToken)
    {
        var achat = await _achats.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = achat.Id }, achat);
    }

    /// <summary>Modifie un achat encore en brouillon.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.AchatsGerer)]
    public async Task<IActionResult> Modifier(int id, AchatRequete requete, CancellationToken cancellationToken)
        => Ok(await _achats.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Confirme un achat auprès du fournisseur.</summary>
    [HttpPost("{id:int}/confirmation")]
    [DroitRequis(PermissionCodes.AchatsGerer)]
    public async Task<IActionResult> Confirmer(int id, CancellationToken cancellationToken)
        => Ok(await _achats.ConfirmerAsync(id, cancellationToken));

    /// <summary>Enregistre la réception des matières : le stock augmente.</summary>
    [HttpPost("{id:int}/reception")]
    [DroitRequis(PermissionCodes.AchatsGerer)]
    public async Task<IActionResult> Receptionner(
        int id, ReceptionAchatRequete requete, CancellationToken cancellationToken)
        => Ok(await _achats.ReceptionnerAsync(id, requete, cancellationToken));

    /// <summary>Annule un achat et inverse les mouvements de stock.</summary>
    [HttpPost("{id:int}/annulation")]
    [DroitRequis(PermissionCodes.AchatsGerer)]
    public async Task<IActionResult> Annuler(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
        => Ok(await _achats.AnnulerAsync(id, requete.Motif, cancellationToken));
}

/// <summary>Motif d'une annulation, conservé dans le journal des opérations.</summary>
public class MotifRequete
{
    public string Motif { get; set; } = string.Empty;
}
