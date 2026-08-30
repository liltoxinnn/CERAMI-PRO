using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Finances;

namespace CeramicWorkshop.Application.Interfaces;

public interface IDepenseService
{
    Task<PagedResult<DepenseDto>> ListerAsync(
        FiltreDepensesRequete requete, CancellationToken cancellationToken = default);

    Task<DepenseDto> CreerAsync(DepenseRequete requete, CancellationToken cancellationToken = default);

    Task<DepenseDto> ModifierAsync(int id, DepenseRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, string motif, CancellationToken cancellationToken = default);

    /// <summary>Total des dépenses sur une période.</summary>
    Task<decimal> TotalAsync(DateTime du, DateTime au, CancellationToken cancellationToken = default);
}

public interface ITableauDeBordService
{
    Task<TableauDeBordDto> ObtenirAsync(CancellationToken cancellationToken = default);
}

public interface IRapportService
{
    Task<RapportDto> GenererAsync(RapportRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Export du rapport au format CSV, lisible par Excel.</summary>
    Task<(string NomFichier, byte[] Contenu)> ExporterCsvAsync(
        RapportRequete requete, CancellationToken cancellationToken = default);
}

/// <summary>
/// Calculs d'aide à la préparation d'une production : surface à couvrir et
/// nombre d'unités à prévoir, perte comprise.
/// </summary>
public interface ICalculateurService
{
    CalculSurfaceDto Surface(CalculSurfaceRequete requete);

    CalculQuantiteDto Quantite(CalculQuantiteRequete requete);
}
