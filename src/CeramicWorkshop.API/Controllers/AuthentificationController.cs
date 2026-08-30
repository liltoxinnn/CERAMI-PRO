using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Connexion, déconnexion et gestion du mot de passe.</summary>
[ApiController]
[Route("api/authentification")]
public class AuthentificationController : ControllerBase
{
    private readonly IAuthService _authentification;

    public AuthentificationController(IAuthService authentification) => _authentification = authentification;

    /// <summary>Connecte un utilisateur et renvoie ses jetons.</summary>
    [HttpPost("connexion")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ConnexionReponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErreurApi), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Connexion(ConnexionRequete requete, CancellationToken cancellationToken)
    {
        var resultat = await _authentification.ConnexionAsync(requete, cancellationToken);

        if (!resultat.Succes)
        {
            return Unauthorized(new ErreurApi { Message = resultat.Message ?? "Connexion impossible." });
        }

        return Ok(resultat.Valeur);
    }

    /// <summary>Renouvelle le jeton d'accès à partir du jeton de renouvellement.</summary>
    [HttpPost("renouvellement")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ConnexionReponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErreurApi), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Renouvellement(RenouvellementRequete requete, CancellationToken cancellationToken)
    {
        var resultat = await _authentification.RenouvelerAsync(requete, cancellationToken);

        if (!resultat.Succes)
        {
            return Unauthorized(new ErreurApi { Message = resultat.Message ?? "Session expirée." });
        }

        return Ok(resultat.Valeur);
    }

    /// <summary>Déconnecte l'utilisateur et invalide son jeton de renouvellement.</summary>
    [HttpPost("deconnexion")]
    [Authorize]
    public async Task<IActionResult> Deconnexion(CancellationToken cancellationToken)
    {
        var resultat = await _authentification.DeconnexionAsync(cancellationToken);
        return Ok(new { message = resultat.Message });
    }

    /// <summary>Profil complet de l'utilisateur connecté, avec ses droits.</summary>
    [HttpGet("profil")]
    [Authorize]
    [ProducesResponseType(typeof(UtilisateurConnecteDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> Profil(CancellationToken cancellationToken)
    {
        var profil = await _authentification.ObtenirProfilAsync(cancellationToken);
        return profil is null
            ? Unauthorized(new ErreurApi { Message = "Session expirée. Veuillez vous reconnecter." })
            : Ok(profil);
    }

    /// <summary>Change le mot de passe de l'utilisateur connecté.</summary>
    [HttpPost("mot-de-passe")]
    [Authorize]
    public async Task<IActionResult> ChangerMotDePasse(
        ChangementMotDePasseRequete requete, CancellationToken cancellationToken)
    {
        var resultat = await _authentification.ChangerMotDePasseAsync(requete, cancellationToken);

        return resultat.Succes
            ? Ok(new { message = resultat.Message })
            : BadRequest(new ErreurApi { Message = resultat.Message ?? "Modification impossible." });
    }
}
