using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Production;

namespace CeramicWorkshop.Application.Interfaces;

/// <summary>Fours de l'atelier.</summary>
public interface IFourService
{
    Task<IReadOnlyList<FourDto>> ListerAsync(CancellationToken cancellationToken = default);

    Task<FourDto> CreerAsync(FourRequete requete, CancellationToken cancellationToken = default);

    Task<FourDto> ModifierAsync(int id, FourRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);
}

/// <summary>Lots de cuisson (fournées).</summary>
public interface ICuissonService
{
    Task<PagedResult<CuissonDto>> ListerAsync(
        FiltreCuissonsRequete requete, CancellationToken cancellationToken = default);

    Task<CuissonDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<CuissonDto> CreerAsync(CuissonRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Démarre la cuisson : le four passe en service.</summary>
    Task<CuissonDto> DemarrerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Défourne : enregistre les pièces intactes et cassées, répartit le coût
    /// énergétique et libère le four.
    /// </summary>
    Task<CuissonDto> DefournerAsync(
        int id, DefournementRequete requete, CancellationToken cancellationToken = default);

    Task<CuissonDto> AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default);
}

/// <summary>Travaux de décoration.</summary>
public interface IDecorationService
{
    Task<PagedResult<DecorationDto>> ListerAsync(
        FiltreDecorationsRequete requete, CancellationToken cancellationToken = default);

    Task<DecorationDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<DecorationDto> CreerAsync(DecorationRequete requete, CancellationToken cancellationToken = default);

    Task<DecorationDto> ModifierAsync(
        int id, DecorationRequete requete, CancellationToken cancellationToken = default);

    Task<DecorationDto> ChangerStatutAsync(
        int id, Domain.Enums.DecorationStatus statut, CancellationToken cancellationToken = default);

    Task<DecorationDto> AjouterPhotoAsync(
        int id, string chemin, string? legende, CancellationToken cancellationToken = default);
}

/// <summary>Contrôles qualité.</summary>
public interface IQualiteService
{
    Task<PagedResult<ControleQualiteDto>> ListerAsync(
        FiltreControlesRequete requete, CancellationToken cancellationToken = default);

    Task<ControleQualiteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<ControleQualiteDto> EnregistrerAsync(
        ControleQualiteRequete requete, CancellationToken cancellationToken = default);
}
