using CeramiPro.Application.DTOs.Referentiels;

namespace CeramiPro.Application.Interfaces;

/// <summary>Gestion des listes simples : catégories et types de décoration.</summary>
public interface IReferentielService
{
    Task<IReadOnlyList<ElementReferentielDto>> ListerAsync(
        TypeReferentiel type, bool inclureInactifs = true, CancellationToken cancellationToken = default);

    Task<ElementReferentielDto> CreerAsync(
        TypeReferentiel type, ElementReferentielRequete requete, CancellationToken cancellationToken = default);

    Task<ElementReferentielDto> ModifierAsync(
        TypeReferentiel type, int id, ElementReferentielRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(TypeReferentiel type, int id, CancellationToken cancellationToken = default);

    /// <summary>Modes de règlement disponibles (espèces, virement, carte, chèque…).</summary>
    Task<IReadOnlyList<ModeReglementDto>> ListerModesReglementAsync(CancellationToken cancellationToken = default);
}

/// <summary>Gestion des unités de mesure.</summary>
public interface IUniteService
{
    Task<IReadOnlyList<UniteDto>> ListerAsync(
        bool inclureInactives = true, CancellationToken cancellationToken = default);

    Task<UniteDto> CreerAsync(UniteRequete requete, CancellationToken cancellationToken = default);

    Task<UniteDto> ModifierAsync(int id, UniteRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);
}
