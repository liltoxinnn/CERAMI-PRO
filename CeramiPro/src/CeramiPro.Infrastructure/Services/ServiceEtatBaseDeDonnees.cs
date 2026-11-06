using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Interroge réellement PostgreSQL. L'écran affiche ainsi l'état constaté,
/// jamais un message d'attente figé.
/// </summary>
public class ServiceEtatBaseDeDonnees : IServiceEtatBaseDeDonnees
{
    /// <summary>Fichier où l'atelier renseigne sa connexion, à côté du programme.</summary>
    public const string FichierConfiguration = "appsettings.Local.json";

    private readonly CeramiProDbContext _contexte;
    private readonly ILogger<ServiceEtatBaseDeDonnees> _journal;

    public ServiceEtatBaseDeDonnees(
        CeramiProDbContext contexte, ILogger<ServiceEtatBaseDeDonnees> journal)
    {
        _contexte = contexte;
        _journal = journal;
    }

    public async Task<EtatBaseDeDonnees> VerifierAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _contexte.Database.CanConnectAsync(cancellationToken)
                ? EtatBaseDeDonnees.Connectee(Connexion().Database ?? string.Empty)
                : EtatBaseDeDonnees.Injoignable();
        }
        catch (Exception erreur)
        {
            _journal.LogWarning(erreur, "La base de données n'a pas répondu.");
            return EtatBaseDeDonnees.Injoignable();
        }
    }

    public async Task<InformationsBaseDeDonnees> DecrireAsync(CancellationToken cancellationToken = default)
    {
        var connexion = Connexion();

        string? versionPostgres = null;
        string? versionSchema = null;
        var enAttente = 0;
        var archivees = 0;
        var disponible = false;

        try
        {
            disponible = await _contexte.Database.CanConnectAsync(cancellationToken);

            if (disponible)
            {
                versionPostgres = await ScalaireAsync("SHOW server_version", cancellationToken);
                versionSchema = (await _contexte.Database.GetAppliedMigrationsAsync(cancellationToken))
                    .LastOrDefault();
                enAttente = (await _contexte.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
                archivees = int.Parse(await ScalaireAsync(
                    "SELECT COUNT(*) FROM pg_tables WHERE schemaname = 'archives'", cancellationToken) ?? "0");
            }
        }
        catch (Exception erreur)
        {
            _journal.LogWarning(erreur, "La description de la base de données a échoué.");
            disponible = false;
        }

        return new InformationsBaseDeDonnees(
            disponible,
            connexion.Host ?? "localhost",
            connexion.Port,
            connexion.Database ?? string.Empty,
            connexion.Username ?? string.Empty,
            versionPostgres,
            versionSchema is null ? null : NomLisible(versionSchema),
            enAttente,
            archivees,
            FichierConfiguration);
    }

    /// <summary>
    /// « 20261106111722_SimplificationDuSchema » devient « Simplification du
    /// schéma (06/11/2026) ».
    /// </summary>
    private static string NomLisible(string migration)
    {
        var separateur = migration.IndexOf('_');

        if (separateur != 14 || !DateTime.TryParseExact(migration[..8], "yyyyMMdd",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out var date))
        {
            return migration;
        }

        var nom = migration[(separateur + 1)..];
        var mots = System.Text.RegularExpressions.Regex.Replace(nom, "(?<=[a-z])(?=[A-Z])", " ");
        mots = mots.Length > 1 ? mots[0] + mots[1..].ToLowerInvariant() : mots;

        return $"{mots} ({date:dd/MM/yyyy})";
    }

    private NpgsqlConnectionStringBuilder Connexion()
        => new(_contexte.Database.GetConnectionString() ?? string.Empty);

    private async Task<string?> ScalaireAsync(string sql, CancellationToken cancellationToken)
    {
        var connexion = _contexte.Database.GetDbConnection();
        var ouverte = connexion.State == System.Data.ConnectionState.Open;

        if (!ouverte)
        {
            await connexion.OpenAsync(cancellationToken);
        }

        try
        {
            await using var commande = connexion.CreateCommand();
            commande.CommandText = sql;
            return Convert.ToString(await commande.ExecuteScalarAsync(cancellationToken),
                System.Globalization.CultureInfo.InvariantCulture);
        }
        finally
        {
            if (!ouverte)
            {
                await connexion.CloseAsync();
            }
        }
    }
}
