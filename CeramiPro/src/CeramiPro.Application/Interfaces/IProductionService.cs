using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;

namespace CeramiPro.Application.Interfaces;

/// <summary>Ordres de production et suivi des étapes de fabrication.</summary>
public interface IProductionService
{
    Task<PagedResult<OrdreProductionDto>> ListerAsync(
        FiltreProductionsRequete requete, CancellationToken cancellationToken = default);

    Task<OrdreProductionDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Tableau de production : les ordres regroupés par étape de fabrication.</summary>
    Task<IReadOnlyList<ColonneProductionDto>> TableauAsync(CancellationToken cancellationToken = default);

    Task<SyntheseProductionDto> SyntheseAsync(CancellationToken cancellationToken = default);

    Task<OrdreProductionDto> CreerAsync(
        OrdreProductionRequete requete, CancellationToken cancellationToken = default);

    Task<OrdreProductionDto> ModifierAsync(
        int id, OrdreProductionRequete requete, CancellationToken cancellationToken = default);

    /// <summary>
    /// Vérifie les matières disponibles puis les consomme (règles n°5 et n°7).
    /// La production passe alors en préparation.
    /// </summary>
    Task<OrdreProductionDto> LancerAsync(
        int id, LancementProductionRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Fait avancer la production à l'étape suivante en enregistrant l'historique.</summary>
    Task<OrdreProductionDto> ChangerEtapeAsync(
        int id, ChangementEtapeRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Annule la production et remet les matières consommées en stock.</summary>
    Task<OrdreProductionDto> AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default);
}
