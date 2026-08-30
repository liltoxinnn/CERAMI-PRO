using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Codes;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Étiquettes des produits et lecture des codes scannés.</summary>
[ApiController]
[Route("api/codes")]
public class CodesController : ControllerBase
{
    private readonly ICodeService _codes;

    public CodesController(ICodeService codes) => _codes = codes;

    /// <summary>Étiquette d'un produit : code QR, code-barres, nom et prix.</summary>
    [HttpGet("produits/{id:int}/etiquette")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Etiquette(int id, CancellationToken cancellationToken)
        => Ok(await _codes.EtiquetteProduitAsync(id, cancellationToken));

    /// <summary>Planche d'étiquettes à imprimer.</summary>
    [HttpPost("etiquettes")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Etiquettes(
        EtiquettesRequete requete, CancellationToken cancellationToken)
        => Ok(await _codes.EtiquettesAsync(requete, cancellationToken));

    /// <summary>
    /// Reconnaît un code scanné et indique l'écran à ouvrir. La recherche se
    /// limite aux modules que l'utilisateur a le droit de consulter.
    /// </summary>
    [HttpGet("scan")]
    [Authorize]
    public async Task<IActionResult> Scanner(
        [FromQuery] string code, CancellationToken cancellationToken)
        => Ok(await _codes.ResoudreAsync(code, cancellationToken));
}
