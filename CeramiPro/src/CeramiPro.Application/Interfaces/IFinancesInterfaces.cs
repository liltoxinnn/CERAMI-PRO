using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.DTOs.Sauvegarde;

namespace CeramiPro.Application.Interfaces;

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

    /// <summary>
    /// Remet la base dans l'état d'une sauvegarde. Toutes les données
    /// enregistrées depuis sont perdues : l'opération est irréversible et
    /// réservée à l'administrateur.
    /// </summary>
    Task<RestaurationDto> RestaurerAsync(
        string nomFichier, CancellationToken cancellationToken = default);
}
