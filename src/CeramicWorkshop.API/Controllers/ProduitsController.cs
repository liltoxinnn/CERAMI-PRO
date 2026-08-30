using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Catalogue;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Catalogue des produits céramiques.</summary>
[ApiController]
[Route("api/produits")]
public class ProduitsController : ControllerBase
{
    private readonly IProduitService _produits;

    public ProduitsController(IProduitService produits) => _produits = produits;

    /// <summary>Liste paginée du catalogue.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreProduitsRequete requete, CancellationToken cancellationToken)
        => Ok(await _produits.ListerAsync(requete, cancellationToken));

    /// <summary>Synthèse du catalogue : nombre de produits, alertes, valeur et marge.</summary>
    [HttpGet("synthese")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Synthese(CancellationToken cancellationToken)
        => Ok(await _produits.SyntheseAsync(cancellationToken));

    /// <summary>Produits finis dont le stock est passé sous le seuil minimum.</summary>
    [HttpGet("stock-faible")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> StockFaible(CancellationToken cancellationToken)
        => Ok(await _produits.ListerStockFaibleAsync(cancellationToken));

    /// <summary>Retrouve un produit à partir d'un code-barres ou d'un QR code scanné.</summary>
    [HttpGet("code/{code}")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> ParCode(string code, CancellationToken cancellationToken)
    {
        var produit = await _produits.RechercherParCodeAsync(code, cancellationToken);

        return produit is null
            ? NotFound(new Application.Common.ErreurApi
            {
                Message = $"Aucun produit ne correspond au code « {code} »."
            })
            : Ok(produit);
    }

    /// <summary>Fiche d'un produit.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _produits.ObtenirAsync(id, cancellationToken));

    /// <summary>Crée un produit.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> Creer(ProduitRequete requete, CancellationToken cancellationToken)
    {
        var produit = await _produits.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = produit.Id }, produit);
    }

    /// <summary>Modifie un produit.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> Modifier(int id, ProduitRequete requete, CancellationToken cancellationToken)
        => Ok(await _produits.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime un produit sans historique.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _produits.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Produit supprimé." });
    }

    /// <summary>Photos d'un produit.</summary>
    [HttpGet("{id:int}/photos")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Photos(int id, CancellationToken cancellationToken)
        => Ok(await _produits.ListerPhotosAsync(id, cancellationToken));

    /// <summary>Ajoute une photo au produit.</summary>
    [HttpPost("{id:int}/photos")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> AjouterPhoto(
        int id, PhotoProduitRequete requete, CancellationToken cancellationToken)
        => Ok(await _produits.AjouterPhotoAsync(id, requete, cancellationToken));

    /// <summary>Supprime une photo.</summary>
    [HttpDelete("{id:int}/photos/{photoId:int}")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> SupprimerPhoto(int id, int photoId, CancellationToken cancellationToken)
    {
        await _produits.SupprimerPhotoAsync(id, photoId, cancellationToken);
        return Ok(new { message = "Photo supprimée." });
    }

    /// <summary>Variantes d'un produit.</summary>
    [HttpGet("{id:int}/variantes")]
    [DroitRequis(PermissionCodes.ProduitsConsulter)]
    public async Task<IActionResult> Variantes(int id, CancellationToken cancellationToken)
        => Ok(await _produits.ListerVariantesAsync(id, cancellationToken));

    /// <summary>Ajoute une variante.</summary>
    [HttpPost("{id:int}/variantes")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> AjouterVariante(
        int id, VarianteProduitRequete requete, CancellationToken cancellationToken)
        => Ok(await _produits.AjouterVarianteAsync(id, requete, cancellationToken));

    /// <summary>Modifie une variante.</summary>
    [HttpPut("{id:int}/variantes/{varianteId:int}")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> ModifierVariante(
        int id, int varianteId, VarianteProduitRequete requete, CancellationToken cancellationToken)
        => Ok(await _produits.ModifierVarianteAsync(id, varianteId, requete, cancellationToken));

    /// <summary>Supprime une variante sans stock.</summary>
    [HttpDelete("{id:int}/variantes/{varianteId:int}")]
    [DroitRequis(PermissionCodes.ProduitsGerer)]
    public async Task<IActionResult> SupprimerVariante(
        int id, int varianteId, CancellationToken cancellationToken)
    {
        await _produits.SupprimerVarianteAsync(id, varianteId, cancellationToken);
        return Ok(new { message = "Variante supprimée." });
    }
}

/// <summary>Recettes de fabrication.</summary>
[ApiController]
[Route("api/recettes")]
public class RecettesController : ControllerBase
{
    private readonly IRecetteService _recettes;

    public RecettesController(IRecetteService recettes) => _recettes = recettes;

    /// <summary>Liste des recettes, éventuellement filtrée par produit.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.RecettesConsulter)]
    public async Task<IActionResult> Lister([FromQuery] int? produitId, CancellationToken cancellationToken)
        => Ok(await _recettes.ListerAsync(produitId, cancellationToken));

    /// <summary>Détail d'une recette.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.RecettesConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _recettes.ObtenirAsync(id, cancellationToken));

    /// <summary>Calcule les matières nécessaires pour produire une quantité donnée.</summary>
    [HttpGet("{id:int}/besoins")]
    [DroitRequis(PermissionCodes.RecettesConsulter)]
    public async Task<IActionResult> Besoins(
        int id, [FromQuery] decimal quantite, CancellationToken cancellationToken)
        => Ok(await _recettes.CalculerBesoinsAsync(id, quantite, cancellationToken));

    /// <summary>Crée une recette.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.RecettesGerer)]
    public async Task<IActionResult> Creer(RecetteRequete requete, CancellationToken cancellationToken)
    {
        var recette = await _recettes.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = recette.Id }, recette);
    }

    /// <summary>Modifie une recette.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.RecettesGerer)]
    public async Task<IActionResult> Modifier(int id, RecetteRequete requete, CancellationToken cancellationToken)
        => Ok(await _recettes.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime une recette jamais utilisée en production.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.RecettesGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _recettes.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Recette supprimée." });
    }
}
