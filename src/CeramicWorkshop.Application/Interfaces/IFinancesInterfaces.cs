using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Finances;
using CeramicWorkshop.Application.DTOs.Alertes;
using CeramicWorkshop.Application.DTOs.Sauvegarde;

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

/// <summary>
/// Centre d'alertes : stock faible, échéances de commandes, retards de
/// production, dettes en attente. Les alertes sont recalculées à la demande.
/// </summary>
public interface IAlerteService
{
    /// <summary>Recalcule les alertes puis renvoie celles qui sont ouvertes.</summary>
    Task<IReadOnlyList<AlerteDto>> ListerAsync(
        FiltreAlertesRequete requete, CancellationToken cancellationToken = default);

    Task<ResumeAlertesDto> ResumeAsync(CancellationToken cancellationToken = default);

    Task MarquerLueAsync(int id, CancellationToken cancellationToken = default);

    Task ToutMarquerLuAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ReglageAlerteDto>> ListerReglagesAsync(CancellationToken cancellationToken = default);

    Task<ReglageAlerteDto> ModifierReglageAsync(
        int id, ReglageAlerteDto reglage, CancellationToken cancellationToken = default);
}

/// <summary>
/// Sauvegarde des données de l'atelier. L'archive produite contient une copie
/// lisible de chaque table, au format CSV, dans un fichier ZIP unique que
/// l'administrateur peut copier sur une clé USB.
/// </summary>
public interface ISauvegardeService
{
    Task<EtatSauvegardeDto> EtatAsync(CancellationToken cancellationToken = default);

    /// <summary>Crée une sauvegarde et renvoie son nom de fichier.</summary>
    Task<SauvegardeDto> CreerAsync(bool automatique = false, CancellationToken cancellationToken = default);

    /// <summary>Relit une sauvegarde existante pour la télécharger.</summary>
    Task<(string NomFichier, byte[] Contenu)> TelechargerAsync(
        string nomFichier, CancellationToken cancellationToken = default);

    Task SupprimerAsync(string nomFichier, CancellationToken cancellationToken = default);

    /// <summary>Supprime les sauvegardes dépassant la durée de conservation.</summary>
    Task<int> PurgerAsync(CancellationToken cancellationToken = default);
}
