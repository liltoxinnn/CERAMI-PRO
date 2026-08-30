using CeramicWorkshop.API.Authorization;
using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>Clients de l'atelier.</summary>
[ApiController]
[Route("api/clients")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clients;

    public ClientsController(IClientService clients) => _clients = clients;

    /// <summary>Liste paginée des clients avec leur solde.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.ClientsConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreClientsRequete requete, CancellationToken cancellationToken)
        => Ok(await _clients.ListerAsync(requete, cancellationToken));

    /// <summary>Clients ayant un montant restant à payer.</summary>
    [HttpGet("dettes")]
    [DroitRequis(PermissionCodes.ClientsConsulter)]
    public async Task<IActionResult> Dettes(CancellationToken cancellationToken)
        => Ok(await _clients.ListerDettesAsync(cancellationToken));

    /// <summary>Fiche d'un client.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.ClientsConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _clients.ObtenirAsync(id, cancellationToken));

    /// <summary>Notes enregistrées sur la fiche d'un client.</summary>
    [HttpGet("{id:int}/notes")]
    [DroitRequis(PermissionCodes.ClientsConsulter)]
    public async Task<IActionResult> Notes(int id, CancellationToken cancellationToken)
        => Ok(await _clients.ListerNotesAsync(id, cancellationToken));

    /// <summary>Ajoute une note à la fiche d'un client.</summary>
    [HttpPost("{id:int}/notes")]
    [DroitRequis(PermissionCodes.ClientsGerer)]
    public async Task<IActionResult> AjouterNote(
        int id, NoteRequete requete, CancellationToken cancellationToken)
        => Ok(await _clients.AjouterNoteAsync(id, requete, cancellationToken));

    /// <summary>Crée un client.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.ClientsGerer)]
    public async Task<IActionResult> Creer(ClientRequete requete, CancellationToken cancellationToken)
    {
        var client = await _clients.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = client.Id }, client);
    }

    /// <summary>Modifie un client.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.ClientsGerer)]
    public async Task<IActionResult> Modifier(int id, ClientRequete requete, CancellationToken cancellationToken)
        => Ok(await _clients.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Supprime un client sans historique.</summary>
    [HttpDelete("{id:int}")]
    [DroitRequis(PermissionCodes.ClientsGerer)]
    public async Task<IActionResult> Supprimer(int id, CancellationToken cancellationToken)
    {
        await _clients.SupprimerAsync(id, cancellationToken);
        return Ok(new { message = "Client supprimé." });
    }
}

/// <summary>Commandes personnalisées.</summary>
[ApiController]
[Route("api/commandes")]
public class CommandesController : ControllerBase
{
    private readonly ICommandeService _commandes;

    public CommandesController(ICommandeService commandes) => _commandes = commandes;

    /// <summary>Liste paginée des commandes personnalisées.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.CommandesConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreCommandesRequete requete, CancellationToken cancellationToken)
        => Ok(await _commandes.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'une commande personnalisée.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.CommandesConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _commandes.ObtenirAsync(id, cancellationToken));

    /// <summary>Crée une commande personnalisée.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.CommandesGerer)]
    public async Task<IActionResult> Creer(CommandeRequete requete, CancellationToken cancellationToken)
    {
        var commande = await _commandes.CreerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = commande.Id }, commande);
    }

    /// <summary>Modifie une commande en cours.</summary>
    [HttpPut("{id:int}")]
    [DroitRequis(PermissionCodes.CommandesGerer)]
    public async Task<IActionResult> Modifier(int id, CommandeRequete requete, CancellationToken cancellationToken)
        => Ok(await _commandes.ModifierAsync(id, requete, cancellationToken));

    /// <summary>Fait avancer la commande à l'étape suivante.</summary>
    [HttpPost("{id:int}/statut")]
    [DroitRequis(PermissionCodes.CommandesGerer)]
    public async Task<IActionResult> ChangerStatut(
        int id, [FromBody] StatutCommandeRequete requete, CancellationToken cancellationToken)
        => Ok(await _commandes.ChangerStatutAsync(id, requete.Statut, cancellationToken));

    /// <summary>Ajoute une photo de référence, un croquis ou une photo de fabrication.</summary>
    [HttpPost("{id:int}/photos")]
    [DroitRequis(PermissionCodes.CommandesGerer)]
    public async Task<IActionResult> AjouterPhoto(
        int id, PhotoCommandeRequete requete, CancellationToken cancellationToken)
        => Ok(await _commandes.AjouterPhotoAsync(id, requete, cancellationToken));

    /// <summary>Ajoute une note à la commande.</summary>
    [HttpPost("{id:int}/notes")]
    [DroitRequis(PermissionCodes.CommandesGerer)]
    public async Task<IActionResult> AjouterNote(
        int id, NoteRequete requete, CancellationToken cancellationToken)
        => Ok(await _commandes.AjouterNoteAsync(id, requete, cancellationToken));

    /// <summary>Annule une commande non livrée.</summary>
    [HttpPost("{id:int}/annulation")]
    [DroitRequis(PermissionCodes.CommandesGerer)]
    public async Task<IActionResult> Annuler(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
        => Ok(await _commandes.AnnulerAsync(id, requete.Motif, cancellationToken));
}

/// <summary>Ventes de produits finis.</summary>
[ApiController]
[Route("api/ventes")]
public class VentesController : ControllerBase
{
    private readonly IVenteService _ventes;

    public VentesController(IVenteService ventes) => _ventes = ventes;

    /// <summary>Liste paginée des ventes.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.VentesConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreVentesRequete requete, CancellationToken cancellationToken)
        => Ok(await _ventes.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'une vente.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.VentesConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _ventes.ObtenirAsync(id, cancellationToken));

    /// <summary>Enregistre une vente : le stock diminue et la facture est émise.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.VentesCreer)]
    public async Task<IActionResult> Enregistrer(VenteRequete requete, CancellationToken cancellationToken)
    {
        var vente = await _ventes.EnregistrerAsync(requete, cancellationToken);
        return CreatedAtAction(nameof(Obtenir), new { id = vente.Id }, vente);
    }

    /// <summary>Annule une vente et remet les produits en stock.</summary>
    [HttpPost("{id:int}/annulation")]
    [DroitRequis(PermissionCodes.VentesAnnuler)]
    public async Task<IActionResult> Annuler(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
        => Ok(await _ventes.AnnulerAsync(id, requete.Motif, cancellationToken));
}

/// <summary>Factures clients.</summary>
[ApiController]
[Route("api/factures")]
public class FacturesController : ControllerBase
{
    private readonly IFactureService _factures;

    public FacturesController(IFactureService factures) => _factures = factures;

    /// <summary>Liste paginée des factures.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.FacturesConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltreFacturesRequete requete, CancellationToken cancellationToken)
        => Ok(await _factures.ListerAsync(requete, cancellationToken));

    /// <summary>Détail d'une facture.</summary>
    [HttpGet("{id:int}")]
    [DroitRequis(PermissionCodes.FacturesConsulter)]
    public async Task<IActionResult> Obtenir(int id, CancellationToken cancellationToken)
        => Ok(await _factures.ObtenirAsync(id, cancellationToken));

    /// <summary>Émet une facture pour une commande personnalisée.</summary>
    [HttpPost("commande")]
    [DroitRequis(PermissionCodes.FacturesEmettre)]
    public async Task<IActionResult> EmettrePourCommande(
        FactureCommandeRequete requete, CancellationToken cancellationToken)
        => Ok(await _factures.EmettrePourCommandeAsync(requete, cancellationToken));
}

/// <summary>Encaissements clients.</summary>
[ApiController]
[Route("api/paiements")]
public class PaiementsController : ControllerBase
{
    private readonly IPaiementService _paiements;

    public PaiementsController(IPaiementService paiements) => _paiements = paiements;

    /// <summary>Liste paginée des paiements.</summary>
    [HttpGet]
    [DroitRequis(PermissionCodes.PaiementsConsulter)]
    public async Task<IActionResult> Lister(
        [FromQuery] FiltrePaiementsRequete requete, CancellationToken cancellationToken)
        => Ok(await _paiements.ListerAsync(requete, cancellationToken));

    /// <summary>Enregistre un encaissement.</summary>
    [HttpPost]
    [DroitRequis(PermissionCodes.PaiementsEnregistrer)]
    public async Task<IActionResult> Enregistrer(PaiementRequete requete, CancellationToken cancellationToken)
        => Ok(await _paiements.EnregistrerAsync(requete, cancellationToken));

    /// <summary>Annule un paiement et corrige les soldes.</summary>
    [HttpPost("{id:int}/annulation")]
    [DroitRequis(PermissionCodes.PaiementsAnnuler)]
    public async Task<IActionResult> Annuler(
        int id, [FromBody] MotifRequete requete, CancellationToken cancellationToken)
    {
        await _paiements.AnnulerAsync(id, requete.Motif, cancellationToken);
        return Ok(new { message = "Paiement annulé." });
    }
}

/// <summary>Nouvelle étape d'une commande personnalisée.</summary>
public class StatutCommandeRequete
{
    public CustomOrderStatus Statut { get; set; }
}
