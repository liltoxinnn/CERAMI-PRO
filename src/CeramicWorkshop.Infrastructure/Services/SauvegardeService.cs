using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Sauvegarde;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Enums;
using CeramicWorkshop.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CeramicWorkshop.Infrastructure.Services;

/// <summary>
/// Sauvegarde des données de l'atelier.
///
/// L'archive produite est un fichier ZIP contenant une copie de chaque table
/// au format CSV. Elle se lit avec n'importe quel tableur, ne dépend d'aucun
/// outil installé sur la machine, et se copie telle quelle sur une clé USB.
/// Les identifiants de connexion à PostgreSQL ne quittent jamais le serveur :
/// ils ne figurent ni dans l'archive, ni dans les écrans.
/// </summary>
public class SauvegardeService : ISauvegardeService
{
    public const string CleAutomatique = "sauvegarde.automatique";
    public const string CleHeure = "sauvegarde.heure";
    public const string CleConservation = "sauvegarde.conservation.jours";

    /// <summary>Préfixe des archives créées automatiquement.</summary>
    public const string PrefixeAutomatique = "ceramipro-auto-";

    /// <summary>Préfixe des archives créées à la demande.</summary>
    public const string PrefixeManuel = "ceramipro-";

    private readonly ApplicationDbContext _context;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;
    private readonly ILogger<SauvegardeService> _journal;
    private readonly string _dossier;

    public SauvegardeService(
        ApplicationDbContext context,
        IDateTimeService horloge,
        IAuditService audit,
        IConfiguration configuration,
        ILogger<SauvegardeService> journal)
    {
        _context = context;
        _horloge = horloge;
        _audit = audit;
        _journal = journal;

        _dossier = configuration["Sauvegarde:Dossier"]
                   ?? Path.Combine(AppContext.BaseDirectory, "sauvegardes");
    }

    public async Task<EtatSauvegardeDto> EtatAsync(CancellationToken cancellationToken = default)
    {
        var reglages = await LireReglagesAsync(cancellationToken);
        var sauvegardes = ListerFichiers();

        return new EtatSauvegardeDto(
            reglages.Automatique,
            reglages.Heure,
            reglages.Conservation,
            _dossier,
            _context.Database.GetDbConnection().Database,
            sauvegardes.Count,
            sauvegardes.Count > 0 ? sauvegardes[0].Date : null,
            sauvegardes);
    }

    public async Task<SauvegardeDto> CreerAsync(
        bool automatique = false, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_dossier);

        var horodatage = _horloge.MaintenantAtelier.ToString("yyyy-MM-dd-HHmm", CultureInfo.InvariantCulture);
        var nom = $"{(automatique ? PrefixeAutomatique : PrefixeManuel)}{horodatage}.zip";
        var chemin = Path.Combine(_dossier, nom);

        // Une sauvegarde par minute suffit : on écrase la précédente si besoin.
        await using (var fichier = File.Create(chemin))
        using (var archive = new ZipArchive(fichier, ZipArchiveMode.Create))
        {
            await EcrireLisezMoiAsync(archive, cancellationToken);

            foreach (var table in TablesDuModele())
            {
                await EcrireTableAsync(archive, table, cancellationToken);
            }
        }

        var information = new FileInfo(chemin);

        await _audit.EnregistrerAsync(AuditAction.Sauvegarde, "Sauvegarde", null,
            $"Sauvegarde {nom} créée ({Taille(information.Length)}).", null, cancellationToken);

        _journal.LogInformation("Sauvegarde créée : {Fichier} ({Taille})", nom, Taille(information.Length));

        return Convertir(information);
    }

    public Task<(string NomFichier, byte[] Contenu)> TelechargerAsync(
        string nomFichier, CancellationToken cancellationToken = default)
    {
        var chemin = CheminSur(nomFichier);

        if (!File.Exists(chemin))
        {
            throw new NotFoundException($"La sauvegarde « {nomFichier} » est introuvable.");
        }

        return Task.FromResult((Path.GetFileName(chemin), File.ReadAllBytes(chemin)));
    }

    public async Task SupprimerAsync(string nomFichier, CancellationToken cancellationToken = default)
    {
        var chemin = CheminSur(nomFichier);

        if (!File.Exists(chemin))
        {
            throw new NotFoundException($"La sauvegarde « {nomFichier} » est introuvable.");
        }

        File.Delete(chemin);

        await _audit.EnregistrerAsync(AuditAction.Suppression, "Sauvegarde", null,
            $"Suppression de la sauvegarde {Path.GetFileName(chemin)}.", null, cancellationToken);
    }

    public async Task<int> PurgerAsync(CancellationToken cancellationToken = default)
    {
        var reglages = await LireReglagesAsync(cancellationToken);

        if (reglages.Conservation <= 0)
        {
            return 0;
        }

        var limite = _horloge.UtcNow.AddDays(-reglages.Conservation);
        var supprimees = 0;

        foreach (var sauvegarde in ListerFichiers().Where(s => s.Date < limite))
        {
            File.Delete(Path.Combine(_dossier, sauvegarde.NomFichier));
            supprimees++;
        }

        if (supprimees > 0)
        {
            _journal.LogInformation("{Nombre} sauvegarde(s) trop ancienne(s) supprimée(s).", supprimees);
        }

        return supprimees;
    }

    // ------------------------------------------------------------- Détails

    /// <summary>Réglages de la sauvegarde, lus dans les paramètres du logiciel.</summary>
    private async Task<(bool Automatique, string Heure, int Conservation)> LireReglagesAsync(
        CancellationToken cancellationToken)
    {
        var cles = new[] { CleAutomatique, CleHeure, CleConservation };

        var reglages = await _context.SystemSettings.AsNoTracking()
            .Where(s => cles.Contains(s.Key))
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        return (
            reglages.TryGetValue(CleAutomatique, out var actif) && bool.TryParse(actif, out var vrai) && vrai,
            reglages.TryGetValue(CleHeure, out var heure) && !string.IsNullOrWhiteSpace(heure) ? heure : "22:00",
            reglages.TryGetValue(CleConservation, out var jours) && int.TryParse(jours, out var valeur)
                ? valeur
                : 30);
    }

    private List<SauvegardeDto> ListerFichiers()
    {
        if (!Directory.Exists(_dossier))
        {
            return new List<SauvegardeDto>();
        }

        return new DirectoryInfo(_dossier)
            .GetFiles("*.zip")
            .OrderByDescending(f => f.LastWriteTimeUtc)
            .Select(Convertir)
            .ToList();
    }

    private static SauvegardeDto Convertir(FileInfo fichier)
        => new(fichier.Name, fichier.Length, Taille(fichier.Length),
            fichier.LastWriteTimeUtc, fichier.Name.StartsWith(PrefixeAutomatique, StringComparison.Ordinal));

    /// <summary>
    /// Empêche de sortir du dossier des sauvegardes : seul le nom du fichier
    /// est accepté, jamais un chemin.
    /// </summary>
    private string CheminSur(string nomFichier)
    {
        var nom = Path.GetFileName(nomFichier ?? string.Empty);

        if (string.IsNullOrWhiteSpace(nom)
            || !nom.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            || nom != nomFichier)
        {
            throw new BusinessRuleException("Nom de sauvegarde invalide.");
        }

        return Path.Combine(_dossier, nom);
    }

    /// <summary>Tables réelles du modèle de données, dans l'ordre alphabétique.</summary>
    private IEnumerable<string> TablesDuModele()
        => _context.Model.GetEntityTypes()
            .Select(type => type.GetTableName())
            .Where(nom => !string.IsNullOrWhiteSpace(nom))
            .Select(nom => nom!)
            .Distinct()
            .OrderBy(nom => nom, StringComparer.Ordinal);

    private async Task EcrireTableAsync(
        ZipArchive archive, string table, CancellationToken cancellationToken)
    {
        var entree = archive.CreateEntry($"donnees/{table}.csv", CompressionLevel.Optimal);

        await using var flux = entree.Open();
        await using var ecriture = new StreamWriter(flux, new UTF8Encoding(true));

        var connexion = _context.Database.GetDbConnection();
        var ouverte = connexion.State == System.Data.ConnectionState.Open;

        if (!ouverte)
        {
            await connexion.OpenAsync(cancellationToken);
        }

        try
        {
            await using var commande = connexion.CreateCommand();

            // Le nom de table provient du modèle EF, jamais d'une saisie. Par
            // précaution supplémentaire, il est vérifié avant d'être employé.
            if (!NomDeTableValide(table))
            {
                throw new InvalidOperationException($"Nom de table inattendu : {table}.");
            }

            commande.CommandText = $"SELECT * FROM \"{table}\"";

            await using var lecteur = await commande.ExecuteReaderAsync(cancellationToken);

            await ecriture.WriteLineAsync(string.Join(';',
                Enumerable.Range(0, lecteur.FieldCount).Select(i => Echapper(lecteur.GetName(i)))));

            while (await lecteur.ReadAsync(cancellationToken))
            {
                await ecriture.WriteLineAsync(string.Join(';',
                    Enumerable.Range(0, lecteur.FieldCount).Select(i => Valeur(lecteur, i))));
            }
        }
        finally
        {
            if (!ouverte)
            {
                await connexion.CloseAsync();
            }
        }
    }

    private async Task EcrireLisezMoiAsync(ZipArchive archive, CancellationToken cancellationToken)
    {
        var entree = archive.CreateEntry("LISEZ-MOI.txt", CompressionLevel.Optimal);

        await using var flux = entree.Open();
        await using var ecriture = new StreamWriter(flux, new UTF8Encoding(true));

        var texte = $"""
            SAUVEGARDE CERAMIPRO
            ====================

            Date de la sauvegarde : {MontantFormatter.FormaterDateHeure(_horloge.MaintenantAtelier)}
            Base de données       : {_context.Database.GetDbConnection().Database}

            Cette archive contient une copie de toutes les données de l'atelier.
            Le dossier « donnees » comprend un fichier par table, au format CSV,
            séparé par des points-virgules et encodé en UTF-8. Chaque fichier
            s'ouvre directement dans un tableur.

            Conservez cette archive en lieu sûr, de préférence sur un support
            différent de l'ordinateur de l'atelier (clé USB, disque externe).

            Pour restaurer les données, reportez-vous au guide de déploiement
            fourni avec le logiciel (docs/DEPLOIEMENT.md).
            """;

        await ecriture.WriteAsync(texte.AsMemory(), cancellationToken);
    }

    private static string Valeur(DbDataReader lecteur, int colonne)
    {
        if (lecteur.IsDBNull(colonne))
        {
            return string.Empty;
        }

        var valeur = lecteur.GetValue(colonne);

        return Echapper(valeur switch
        {
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            decimal nombre => nombre.ToString(CultureInfo.InvariantCulture),
            double nombre => nombre.ToString(CultureInfo.InvariantCulture),
            float nombre => nombre.ToString(CultureInfo.InvariantCulture),
            bool booleen => booleen ? "true" : "false",
            byte[] octets => Convert.ToBase64String(octets),
            _ => valeur.ToString() ?? string.Empty
        });
    }

    /// <summary>Seuls les noms de tables simples sont acceptés.</summary>
    private static bool NomDeTableValide(string table)
        => table.Length is > 0 and <= 63
           && char.IsLetter(table[0])
           && table.All(c => char.IsLetterOrDigit(c) || c == '_');

    private static string Echapper(string valeur)
        => valeur.Contains(';') || valeur.Contains('"') || valeur.Contains('\n') || valeur.Contains('\r')
            ? $"\"{valeur.Replace("\"", "\"\"")}\""
            : valeur;

    private static string Taille(long octets) => octets switch
    {
        < 1024 => $"{octets} octets",
        < 1024 * 1024 => $"{octets / 1024d:0.#} Ko",
        _ => $"{octets / (1024d * 1024d):0.#} Mo"
    };
}
