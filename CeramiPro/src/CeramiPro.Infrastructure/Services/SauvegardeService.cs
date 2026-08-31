using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Sauvegarde;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Services;

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

    private readonly CeramiProDbContext _context;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;
    private readonly ILogger<SauvegardeService> _journal;
    private readonly string _dossier;

    public SauvegardeService(
        CeramiProDbContext context,
        IServiceDateHeure horloge,
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
            throw new IntrouvableException($"La sauvegarde « {nomFichier} » est introuvable.");
        }

        return Task.FromResult((Path.GetFileName(chemin), File.ReadAllBytes(chemin)));
    }

    public async Task SupprimerAsync(string nomFichier, CancellationToken cancellationToken = default)
    {
        var chemin = CheminSur(nomFichier);

        if (!File.Exists(chemin))
        {
            throw new IntrouvableException($"La sauvegarde « {nomFichier} » est introuvable.");
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

        var limite = _horloge.MaintenantUtc.AddDays(-reglages.Conservation);
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
            throw new RegleMetierException("Nom de sauvegarde invalide.");
        }

        return Path.Combine(_dossier, nom);
    }


    // ---------------------------------------------------------- Restauration

    /// <summary>
    /// Sous PostgreSQL, ce réglage suspend le contrôle des clés étrangères le
    /// temps de la restauration : les tables se remplissent alors dans
    /// n'importe quel ordre, sans avoir à deviner lequel.
    /// </summary>
    private const string SuspendreLesLiens = "SET session_replication_role = 'replica'";

    private const string RetablirLesLiens = "SET session_replication_role = 'origin'";

    public async Task<RestaurationDto> RestaurerAsync(
        string nomFichier, CancellationToken cancellationToken = default)
    {
        var chemin = CheminSur(nomFichier);

        if (!File.Exists(chemin))
        {
            throw new IntrouvableException($"La sauvegarde « {nomFichier} » est introuvable.");
        }

        using var archive = ZipFile.OpenRead(chemin);

        var tables = TablesDuModele().ToList();
        var connues = tables.ToHashSet(StringComparer.Ordinal);

        var contenus = archive.Entries
            .Where(e => e.FullName.StartsWith("donnees/", StringComparison.Ordinal)
                        && e.FullName.EndsWith(".csv", StringComparison.Ordinal))
            .ToDictionary(
                e => Path.GetFileNameWithoutExtension(e.FullName),
                e => e,
                StringComparer.Ordinal);

        if (contenus.Count == 0)
        {
            throw new RegleMetierException(
                $"Le fichier « {nomFichier} » ne contient aucune donnée : "
                + "ce n'est pas une sauvegarde CeramiPro.");
        }

        // Une archive qui parle de tables que ce logiciel ne connaît pas vient
        // d'une autre version : la restaurer laisserait la base incohérente.
        var etrangeres = contenus.Keys.Where(nom => !connues.Contains(nom)).ToList();

        if (etrangeres.Count > 0)
        {
            throw new RegleMetierException(
                $"La sauvegarde « {nomFichier} » comporte des tables inconnues de cette "
                + "version du logiciel : " + string.Join(", ", etrangeres.Take(5)) + ". "
                + "Elle a probablement été produite par une version plus récente.");
        }

        var connexion = _context.Database.GetDbConnection();
        var ouverte = connexion.State == System.Data.ConnectionState.Open;

        if (!ouverte)
        {
            await connexion.OpenAsync(cancellationToken);
        }

        var lignesRestaurees = 0;
        var tablesRestaurees = 0;

        try
        {
            await using var transaction = await connexion.BeginTransactionAsync(cancellationToken);

            try
            {
                await ExecuterAsync(connexion, transaction, SuspendreLesLiens, cancellationToken);
            }
            catch (DbException erreur)
            {
                throw new RegleMetierException(
                    "La restauration demande des droits que le compte de base de données ne "
                    + "possède pas. Connectez-vous à PostgreSQL avec le compte administrateur, "
                    + "puis recommencez.\n\nDétail : " + erreur.Message);
            }

            // Toutes les tables sont vidées d'abord : restaurer une archive
            // partielle ne doit pas laisser d'anciennes lignes derrière elle.
            foreach (var table in tables)
            {
                await ExecuterAsync(
                    connexion, transaction, $"TRUNCATE TABLE \"{table}\" CASCADE", cancellationToken);
            }

            foreach (var table in tables.Where(contenus.ContainsKey))
            {
                var lignes = await RemplirTableAsync(
                    connexion, transaction, table, contenus[table], cancellationToken);

                lignesRestaurees += lignes;
                tablesRestaurees++;
            }

            // Les compteurs d'identifiants doivent repartir après la dernière
            // ligne remise en place, sans quoi le prochain enregistrement
            // entrerait en collision avec une fiche restaurée.
            foreach (var table in await TablesAvecCompteurAsync(connexion, transaction, cancellationToken))
            {
                await RemettreLeCompteurAsync(connexion, transaction, table, cancellationToken);
            }

            await ExecuterAsync(connexion, transaction, RetablirLesLiens, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            if (!ouverte)
            {
                await connexion.CloseAsync();
            }
        }

        _journal.LogInformation(
            "Base restaurée depuis « {Fichier} » : {Tables} tables, {Lignes} lignes.",
            nomFichier, tablesRestaurees, lignesRestaurees);

        await _audit.EnregistrerAsync(
            AuditAction.Restauration, "Sauvegarde", nomFichier,
            $"Restauration de {tablesRestaurees} table(s) et {lignesRestaurees} ligne(s).",
            cancellationToken: cancellationToken);

        return new RestaurationDto(
            Path.GetFileName(chemin),
            File.GetLastWriteTime(chemin),
            tablesRestaurees,
            lignesRestaurees);
    }

    private static async Task ExecuterAsync(
        DbConnection connexion, DbTransaction transaction, string sql,
        CancellationToken cancellationToken)
    {
        await using var commande = connexion.CreateCommand();
        commande.Transaction = transaction;
        commande.CommandText = sql;

        await commande.ExecuteNonQueryAsync(cancellationToken);
    }

    /// <summary>
    /// Réinsère les lignes d'une table. Les valeurs sont transmises comme du
    /// texte : c'est PostgreSQL qui les convertit au type de chaque colonne,
    /// ce qui évite d'avoir à deviner ce type depuis le fichier.
    /// </summary>
    private static async Task<int> RemplirTableAsync(
        DbConnection connexion,
        DbTransaction transaction,
        string table,
        ZipArchiveEntry entree,
        CancellationToken cancellationToken)
    {
        await using var flux = entree.Open();
        using var lecture = new StreamReader(flux, new UTF8Encoding(true));

        if (await lecture.ReadLineAsync(cancellationToken) is not { } enTete)
        {
            return 0;
        }

        var colonnes = DecouperLigne(enTete)
            .Select(c => c.Valeur ?? string.Empty)
            .ToList();

        if (colonnes.Count == 0 || colonnes.Any(c => !NomDeTableValide(c)))
        {
            throw new RegleMetierException(
                $"Les colonnes de la table « {table} » ne sont pas lisibles dans cette sauvegarde.");
        }

        var noms = string.Join(", ", colonnes.Select(c => $"\"{c}\""));
        var marques = string.Join(", ", colonnes.Select((_, rang) => $"@p{rang}"));
        var sql = $"INSERT INTO \"{table}\" ({noms}) VALUES ({marques})";

        var lignes = 0;

        while (await LireLigneAsync(lecture, cancellationToken) is { } ligne)
        {
            var valeurs = DecouperLigne(ligne);

            if (valeurs.Count != colonnes.Count)
            {
                throw new RegleMetierException(
                    $"Une ligne de la table « {table} » ne comporte pas le bon nombre de colonnes : "
                    + "la sauvegarde est abîmée.");
            }

            await using var commande = connexion.CreateCommand();
            commande.Transaction = transaction;
            commande.CommandText = sql;

            for (var rang = 0; rang < valeurs.Count; rang++)
            {
                var parametre = commande.CreateParameter();
                parametre.ParameterName = $"@p{rang}";

                // Une case vide représente une valeur absente ; deux
                // guillemets, un texte vide.
                parametre.Value = valeurs[rang].Valeur is null
                    ? DBNull.Value
                    : valeurs[rang].Valeur!;

                if (parametre is Npgsql.NpgsqlParameter npgsql)
                {
                    npgsql.NpgsqlDbType = NpgsqlTypes.NpgsqlDbType.Unknown;
                }

                commande.Parameters.Add(parametre);
            }

            await commande.ExecuteNonQueryAsync(cancellationToken);
            lignes++;
        }

        return lignes;
    }

    /// <summary>
    /// Lit une ligne du fichier, en tenant compte des valeurs qui contiennent
    /// elles-mêmes un retour à la ligne, entre guillemets.
    /// </summary>
    private static async Task<string?> LireLigneAsync(
        StreamReader lecture, CancellationToken cancellationToken)
    {
        if (await lecture.ReadLineAsync(cancellationToken) is not { } ligne)
        {
            return null;
        }

        while (ligne.Count(c => c == '"') % 2 != 0)
        {
            if (await lecture.ReadLineAsync(cancellationToken) is not { } suite)
            {
                break;
            }

            ligne += "\n" + suite;
        }

        return ligne;
    }

    /// <summary>
    /// Découpe une ligne en valeurs. Une valeur absente et un texte vide sont
    /// distingués : la première n'est pas entre guillemets, le second l'est.
    /// </summary>
    private static List<(string? Valeur, bool Cite)> DecouperLigne(string ligne)
    {
        var valeurs = new List<(string?, bool)>();
        var courante = new StringBuilder();
        var entreGuillemets = false;
        var cite = false;

        for (var rang = 0; rang < ligne.Length; rang++)
        {
            var caractere = ligne[rang];

            if (entreGuillemets)
            {
                if (caractere != '"')
                {
                    courante.Append(caractere);
                }
                else if (rang + 1 < ligne.Length && ligne[rang + 1] == '"')
                {
                    courante.Append('"');
                    rang++;
                }
                else
                {
                    entreGuillemets = false;
                }

                continue;
            }

            switch (caractere)
            {
                case '"':
                    entreGuillemets = true;
                    cite = true;
                    break;

                case ';':
                    valeurs.Add((cite || courante.Length > 0 ? courante.ToString() : null, cite));
                    courante.Clear();
                    cite = false;
                    break;

                default:
                    courante.Append(caractere);
                    break;
            }
        }

        valeurs.Add((cite || courante.Length > 0 ? courante.ToString() : null, cite));

        return valeurs;
    }

    /// <summary>
    /// Tables dont la colonne « Id » est attribuée automatiquement.
    ///
    /// La question est posée au catalogue plutôt que table par table : les
    /// tables de liaison n'ont pas de colonne « Id », et PostgreSQL refuse
    /// une requête qui la nommerait, sans se contenter de répondre « rien ».
    /// </summary>
    private static async Task<IReadOnlyList<string>> TablesAvecCompteurAsync(
        DbConnection connexion, DbTransaction transaction, CancellationToken cancellationToken)
    {
        await using var commande = connexion.CreateCommand();
        commande.Transaction = transaction;
        commande.CommandText = """
            SELECT table_name
            FROM information_schema.columns
            WHERE table_schema = 'public'
              AND column_name = 'Id'
              AND (is_identity = 'YES' OR column_default LIKE 'nextval%')
            ORDER BY table_name
            """;

        var tables = new List<string>();

        await using var lecteur = await commande.ExecuteReaderAsync(cancellationToken);

        while (await lecteur.ReadAsync(cancellationToken))
        {
            tables.Add(lecteur.GetString(0));
        }

        return tables;
    }

    /// <summary>Remet le compteur d'identifiants après la dernière ligne restaurée.</summary>
    private static async Task RemettreLeCompteurAsync(
        DbConnection connexion, DbTransaction transaction, string table,
        CancellationToken cancellationToken)
    {
        if (!NomDeTableValide(table))
        {
            return;
        }

        await using var commande = connexion.CreateCommand();
        commande.Transaction = transaction;

        // Le troisième paramètre indique si le compteur a déjà servi : sur une
        // table vide, le prochain identifiant doit rester 1.
        commande.CommandText = $"""
            SELECT setval(
                pg_get_serial_sequence('"{table}"', 'Id'),
                COALESCE((SELECT MAX("Id") FROM "{table}"), 1),
                (SELECT COUNT(*) FROM "{table}") > 0)
            """;

        await commande.ExecuteNonQueryAsync(cancellationToken);
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

            Date de la sauvegarde : {Formatage.DateHeure(_horloge.MaintenantAtelier)}
            Base de données       : {_context.Database.GetDbConnection().Database}

            Cette archive contient une copie de toutes les données de l'atelier.
            Le dossier « donnees » comprend un fichier par table, au format CSV,
            séparé par des points-virgules et encodé en UTF-8. Chaque fichier
            s'ouvre directement dans un tableur.

            Conservez cette archive en lieu sûr, de préférence sur un support
            différent de l'ordinateur de l'atelier (clé USB, disque externe).

            Pour remettre ces données en place, ouvrez CeramiPro, allez dans
            « Administration », puis « Sauvegarde », choisissez cette archive
            dans la liste et cliquez sur « Restaurer ».

            La restauration remplace TOUTES les données actuelles par celles de
            l'archive : sauvegardez l'état présent avant de vous en servir.
            """;

        await ecriture.WriteAsync(texte.AsMemory(), cancellationToken);
    }

    /// <summary>
    /// Écrit une valeur dans le fichier.
    ///
    /// Une case laissée vide représente une valeur absente, et deux
    /// guillemets un texte vide : sans cette distinction, la restauration ne
    /// saurait pas laquelle des deux remettre en place.
    /// </summary>
    private static string Valeur(DbDataReader lecteur, int colonne)
    {
        if (lecteur.IsDBNull(colonne))
        {
            return string.Empty;
        }

        var valeur = lecteur.GetValue(colonne);

        if (valeur is string texte && texte.Length == 0)
        {
            return "\"\"";
        }

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
