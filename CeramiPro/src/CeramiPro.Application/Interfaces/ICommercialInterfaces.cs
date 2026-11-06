using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.Interfaces;

public interface IClientService
{
    Task<PagedResult<ClientDto>> ListerAsync(
        FiltreClientsRequete requete, CancellationToken cancellationToken = default);

    Task<ClientDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tous les clients actifs, par ordre alphabétique, pour les listes de choix.</summary>
    Task<IReadOnlyList<ClientChoixDto>> ListerPourChoixAsync(CancellationToken cancellationToken = default);

    Task<ClientDto> CreerAsync(ClientRequete requete, CancellationToken cancellationToken = default);

    Task<ClientDto> ModifierAsync(int id, ClientRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<NoteClientDto>> ListerNotesAsync(int id, CancellationToken cancellationToken = default);

    Task<NoteClientDto> AjouterNoteAsync(int id, NoteRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Clients ayant un solde restant à payer.</summary>
    Task<IReadOnlyList<DetteClientDto>> ListerDettesAsync(CancellationToken cancellationToken = default);
}

public interface ICommandeService
{
    Task<PagedResult<CommandeDto>> ListerAsync(
        FiltreCommandesRequete requete, CancellationToken cancellationToken = default);

    Task<CommandeDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<CommandeDto> CreerAsync(CommandeRequete requete, CancellationToken cancellationToken = default);

    Task<CommandeDto> ModifierAsync(int id, CommandeRequete requete, CancellationToken cancellationToken = default);

    Task<CommandeDto> ChangerStatutAsync(
        int id, CustomOrderStatus statut, CancellationToken cancellationToken = default);

    Task<CommandeDto> AjouterPhotoAsync(
        int id, PhotoCommandeRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Retire une photo et renvoie le chemin du fichier, à effacer ensuite.</summary>
    Task<string> SupprimerPhotoAsync(int id, int photoId, CancellationToken cancellationToken = default);

    Task<CommandeDto> AjouterNoteAsync(int id, NoteRequete requete, CancellationToken cancellationToken = default);

    /// <summary>
    /// Annule une commande. Un acompte déjà versé bloque l'annulation, sauf à
    /// le rembourser : les paiements sont alors annulés dans le même geste.
    /// </summary>
    Task<CommandeDto> AnnulerAsync(
        int id, string motif, bool rembourser = false, CancellationToken cancellationToken = default);
}

public interface IVenteService
{
    Task<PagedResult<VenteDto>> ListerAsync(
        FiltreVentesRequete requete, CancellationToken cancellationToken = default);

    Task<VenteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Enregistre une vente : le stock diminue et le reçu peut être imprimé.</summary>
    Task<VenteDto> EnregistrerAsync(VenteRequete requete, CancellationToken cancellationToken = default);

    /// <summary>
    /// Annule une vente et remet les produits en stock. Une vente déjà payée
    /// ne s'annule qu'en remboursant le client : ses paiements sont alors
    /// annulés dans le même geste, rien ne reste à moitié fait.
    /// </summary>
    Task<VenteDto> AnnulerAsync(
        int id, string motif, bool rembourser = false, CancellationToken cancellationToken = default);
}

public interface IPaiementService
{
    Task<PagedResult<PaiementDto>> ListerAsync(
        FiltrePaiementsRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Enregistre un encaissement et met à jour les soldes concernés.</summary>
    Task<PaiementDto> EnregistrerAsync(PaiementRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Annule un paiement (suppression logique) et corrige les soldes.</summary>
    Task AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default);
}
