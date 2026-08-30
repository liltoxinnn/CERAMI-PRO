using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Gestion des comptes utilisateurs.</summary>
[ApiController]
[Route("api/utilisateurs")]
public class UtilisateursController : ControllerBase
{
    private readonly IUtilisateurService _utilisateurs;

    public UtilisateursController(IUtilisateurService utilisateurs) => _utilisateurs = utilisateurs;

    /// <summary>Liste paginée des utilisateurs.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.UtilisateursConsulter)]
    [ProducesResponseType(typeof(PagedResult<UtilisateurDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> Lister([FromQuery] PagedRequest requete, CancellationToken cancellationToken)
        => Ok(await _utilisateurs.ListerAsync(requete, cancellationToken));

    /// <summary>Fiche d'un utilisateur.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.UtilisateursConsulter)]
    [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErreurApi), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _utilisateurs.ObtenirAsync(id, cancellationToken));

    /// <summary>Crée un utilisateur.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.UtilisateursGerer)]
    [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErreurApi), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Creer(CreerUtilisateurRequete requete, CancellationToken cancellationToken)
    {
        var utilisateur = await _utilisateurs.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = utilisateur.Id }, utilisateur);
    }

    /// <summary>Modifie un utilisateur.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.UtilisateursGerer)]
    [ProducesResponseType(typeof(UtilisateurDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Modifier(
        int id, ModifierUtilisateurRequete requete, CancellationToken cancellationToken)
        => Ok(await _utilisateurs.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Réinitialise le mot de passe d'un utilisateur.</summary>
    [HttpPost("{id:int}/mot-de-passe")]
    [DroitRequis(PermissionCodes.UtilisateursGerer)]
    public async Task<IActionResult> ReinitialiserMotDePasse(
        int id, ReinitialiserMotDePasseRequete requete, CancellationToken cancellationToken)
    {
        await _utilisateurs.ReinitialiserMotDePasseAsync(id, requete, cancellationToken);
        return Ok(new { message = "Mot de passe réinitialisé." });
    }

    /// <summary>Active un compte utilisateur.</summary>
    [HttpPost("{id:int}/activation")]
    [DroitRequis(PermissionCodes.UtilisateursGerer)]
    public async Task<IActionResult> Activer(int id, CancellationToken cancellationToken)
    {
        await _utilisateurs.ChangerActivationAsync(id, true, cancellationToken);
        return Ok(new { message = "Compte activé." });
    }

    /// <summary>Désactive un compte utilisateur.</summary>
    [HttpPost("{id:int}/desactivation")]
    [DroitRequis(PermissionCodes.UtilisateursGerer)]
    public async Task<IActionResult> Desactiver(int id, CancellationToken cancellationToken)
    {
        await _utilisateurs.ChangerActivationAsync(id, false, cancellationToken);
        return Ok(new { message = "Compte désactivé." });
    }
}
