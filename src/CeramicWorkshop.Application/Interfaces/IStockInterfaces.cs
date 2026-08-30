using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Stock;

namespace CeramicWorkshop.Application.Interfaces;

public interface IMatiereService
{
    Task<PagedResult<MatiereDto>> ListerAsync(
        FiltreMatieresRequete requete, CancellationToken cancellationToken = default);

    Task<MatiereDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<MatiereDto> CreerAsync(MatiereRequete requete, CancellationToken cancellationToken = default);

    Task<MatiereDto> ModifierAsync(int id, MatiereRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);

    Task<SyntheseStockDto> SyntheseAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<LotMatiereDto>> ListerLotsAsync(int matiereId, CancellationToken cancellationToken = default);

    /// <summary>Matières dont le stock est passé sous le seuil minimum.</summary>
    Task<IReadOnlyList<MatiereDto>> ListerStockFaibleAsync(CancellationToken cancellationToken = default);
}

public interface IFournisseurService
{
    Task<PagedResult<FournisseurDto>> ListerAsync(
        FiltreFournisseursRequete requete, CancellationToken cancellationToken = default);

    Task<FournisseurDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<FournisseurDto> CreerAsync(FournisseurRequete requete, CancellationToken cancellationToken = default);

    Task<FournisseurDto> ModifierAsync(int id, FournisseurRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReglementFournisseurDto>> ListerReglementsAsync(
        int fournisseurId, CancellationToken cancellationToken = default);

    Task<ReglementFournisseurDto> EnregistrerReglementAsync(
        ReglementFournisseurRequete requete, CancellationToken cancellationToken = default);
}

public interface IAchatService
{
    Task<PagedResult<AchatDto>> ListerAsync(
        FiltreAchatsRequete requete, CancellationToken cancellationToken = default);

    Task<AchatDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<AchatDto> CreerAsync(AchatRequete requete, CancellationToken cancellationToken = default);

    Task<AchatDto> ModifierAsync(int id, AchatRequete requete, CancellationToken cancellationToken = default);

    Task<AchatDto> ConfirmerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Enregistre la réception : les matières entrent en stock.</summary>
    Task<AchatDto> ReceptionnerAsync(
        int id, ReceptionAchatRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Annule l'achat et inverse les mouvements de stock déjà enregistrés.</summary>
    Task<AchatDto> AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default);
}
