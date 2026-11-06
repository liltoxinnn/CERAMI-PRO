using CeramiPro.Application.DTOs.Referentiels;

namespace CeramiPro.Application.Interfaces;

/// <summary>Gestion des listes simples : catégories de produits et de dépenses.</summary>
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
