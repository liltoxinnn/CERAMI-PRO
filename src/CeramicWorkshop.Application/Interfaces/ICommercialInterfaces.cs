using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.Application.Interfaces;

public interface IClientService
{
    Task<PagedResult<ClientDto>> ListerAsync(
        FiltreClientsRequete requete, CancellationToken cancellationToken = default);

    Task<ClientDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

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

    Task<CommandeDto> AjouterNoteAsync(int id, NoteRequete requete, CancellationToken cancellationToken = default);

    Task<CommandeDto> AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default);
}

public interface IVenteService
{
    Task<PagedResult<VenteDto>> ListerAsync(
        FiltreVentesRequete requete, CancellationToken cancellationToken = default);

    Task<VenteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Enregistre une vente : le stock diminue et la facture est émise.</summary>
    Task<VenteDto> EnregistrerAsync(VenteRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Annule une vente et remet les produits en stock.</summary>
    Task<VenteDto> AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default);
}

public interface IFactureService
{
    Task<PagedResult<FactureDto>> ListerAsync(
        FiltreFacturesRequete requete, CancellationToken cancellationToken = default);

    Task<FactureDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Émet une facture pour une commande personnalisée.</summary>
    Task<FactureDto> EmettrePourCommandeAsync(
        FactureCommandeRequete requete, CancellationToken cancellationToken = default);
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
