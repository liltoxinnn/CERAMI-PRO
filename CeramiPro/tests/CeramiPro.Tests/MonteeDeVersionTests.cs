using System.Data.Common;
using System.Globalization;
using System.IO.Compression;
using System.Text;
using CeramiPro.Application;
using CeramiPro.Application.DTOs.Sauvegarde;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CeramiPro.Tests;

/// <summary>
/// Passage de l'ancienne version (49 tables) à la version simplifiée
/// (19 tables), sur un vrai PostgreSQL.
///
/// Un atelier fictif est enregistré avec l'ancien schéma — voir
/// « Donnees/ancienne-version.sql », qui contient chacun des cas délicats —
/// puis il est mis à jour de deux façons : par la migration, comme au premier
/// démarrage de la nouvelle version, et par la restauration d'une sauvegarde
/// faite avec l'ancienne version. Dans les deux cas, rien de ce que l'atelier
/// utilise encore ne doit manquer ni changer de sens.
/// </summary>
[Collection(CollectionPostgres.Nom)]
public class MonteeDeVersionTests : IDisposable
{
    private const string NomBase = "CeramiProDB_Montee";
    private const string AncienneMigration = "20260831052649_MigrationInitiale";

    private readonly string _dossier = Path.Combine(
        Path.GetTempPath(), $"ceramipro-montee-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    // ------------------------------------------------------------ Migration

    [PostgresFact]
    public async Task La_migration_conserve_les_donnees_de_l_ancienne_version()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        try
        {
            await PreparerAncienneBaseAsync(contexte);

            await contexte.Database.MigrateAsync();

            await VerifierLaConversionAsync(contexte);

            // Les données des modules retirés ne sont pas perdues : elles
            // sont rangées dans le schéma « archives » de la même base.
            var archives = await ListerAsync(contexte,
                "SELECT table_name FROM information_schema.tables WHERE table_schema = 'archives'");

            archives.Should().Contain(new[]
            {
                "Invoices", "Materials", "Suppliers", "Purchases", "ProductVariants", "StockAdjustments",
                "Users_avant_simplification", "Products_avant_simplification",
                "InventoryTransactions_avant_simplification"
            });

            // Une table vide d'un module retiré n'encombre pas les archives,
            // et les droits et rôles, remplacés, ne sont pas gardés.
            archives.Should().NotContain(new[] { "Kilns", "Roles", "Permissions", "RolePermissions" });

            archives.Should().Contain("CustomOrders_avant_simplification");

            (await CompterAsync(contexte,
                    """SELECT COUNT(*) FROM archives."InventoryTransactions_avant_simplification" WHERE "MaterialId" IS NOT NULL"""))
                .Should().Be(2, "les mouvements de matières restent consultables");

            (await CompterAsync(contexte, """
                SELECT COUNT(*) FROM pg_constraint c
                JOIN pg_class s ON s.oid = c.conrelid JOIN pg_namespace ns ON ns.oid = s.relnamespace
                JOIN pg_class t ON t.oid = c.confrelid JOIN pg_namespace nt ON nt.oid = t.relnamespace
                WHERE c.contype = 'f' AND ns.nspname = 'archives' AND nt.nspname <> 'archives'
                """)).Should().Be(0, "les archives ne doivent rien bloquer dans les tables vivantes");

            // Le schéma public ne contient plus que les 19 tables du modèle.
            var tablesPubliques = await ListerAsync(contexte, """
                SELECT table_name FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
                """);

            tablesPubliques.Should().HaveCount(19);
            tablesPubliques.Should().BeEquivalentTo(
                contexte.Model.GetEntityTypes().Select(t => t.GetTableName()));

            // Supprimer un client que référence une ancienne facture archivée
            // doit rester possible.
            await ExecuterAsync(contexte, """INSERT INTO "Customers" ("CustomerNumber", "FullName", "IsActive", "CreatedAt") VALUES ('CLI-TEST', 'Passage', TRUE, now())""");
            await ExecuterAsync(contexte, """DELETE FROM "Customers" WHERE "CustomerNumber" = 'CLI-TEST'""");

            // Le logiciel travaille ensuite normalement sur la base mise à jour :
            // une vente diminue le stock, et la numérotation continue à la suite.
            var admin = await contexte.Users.AsNoTracking().SingleAsync(u => u.UserName == "admin");
            fournisseur.GetRequiredService<ISessionAtelier>()
                .Ouvrir(admin.Id, admin.UserName, admin.FullName, admin.Role);

            var stockAvant = (await contexte.Products.AsNoTracking().SingleAsync(p => p.Id == 1)).CurrentStock;

            var vente = await fournisseur.GetRequiredService<IVenteService>().EnregistrerAsync(
                new Application.DTOs.Commercial.VenteRequete
                {
                    ClientId = 1,
                    Lignes = new() { new() { ProduitId = 1, Quantite = 1m } }
                });

            vente.Numero.Should().NotBe("VTE-2026-0001");
            (await contexte.Products.AsNoTracking().SingleAsync(p => p.Id == 1)).CurrentStock
                .Should().Be(stockAvant - 1m);

            var client = await fournisseur.GetRequiredService<IClientService>()
                .CreerAsync(new Application.DTOs.Commercial.ClientRequete { Nom = "Client après la mise à jour" });
            client.Numero.Should().NotBe("CLI-2026-0001");
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Une_installation_neuve_n_a_que_les_tables_du_modele()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        try
        {
            await contexte.Database.EnsureDeletedAsync();
            await contexte.Database.MigrateAsync();

            var tables = await ListerAsync(contexte, """
                SELECT table_schema || '.' || table_name FROM information_schema.tables
                WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
                  AND table_name <> '__EFMigrationsHistory'
                """);

            tables.Should().HaveCount(19).And.OnlyContain(t => t.StartsWith("public."));

            (await CompterAsync(contexte, "SELECT COUNT(*) FROM pg_namespace WHERE nspname = 'archives'"))
                .Should().Be(0, "une base neuve n'a rien à archiver");
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    // ---------------------------------------------------------- Restauration

    [PostgresFact]
    public async Task Une_sauvegarde_de_l_ancienne_version_se_restaure_avec_les_memes_conversions()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        try
        {
            // Sauvegarde produite par l'ancienne version, depuis l'ancien schéma.
            await PreparerAncienneBaseAsync(contexte);
            const string fichier = "ceramipro-2026-10-30-1800.zip";
            await EcrireSauvegardeAncienneVersionAsync(contexte, Path.Combine(_dossier, fichier));

            // La base passe à la nouvelle version, puis on y restaure
            // l'ancienne sauvegarde : tout ce qui est en place est remplacé.
            await contexte.Database.MigrateAsync();
            await ExecuterAsync(contexte, """UPDATE "Products" SET "CurrentStock" = 0, "Name" = 'Écrasé'""");

            fournisseur.GetRequiredService<ISessionAtelier>()
                .Ouvrir(1, "admin", "Administrateur", RoleUtilisateur.Administrateur);

            RestaurationDto resultat;

            using (var portee = fournisseur.CreateScope())
            {
                resultat = await portee.ServiceProvider.GetRequiredService<ISauvegardeService>()
                    .RestaurerAsync(fichier);
            }

            resultat.AncienneVersion.Should().BeTrue();
            resultat.TablesNonReprises.Should().Be(30, "les 30 tables des modules retirés restent dans le fichier");
            resultat.NombreTables.Should().Be(19);

            contexte.ChangeTracker.Clear();
            await VerifierLaConversionAsync(contexte);
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    // ------------------------------------------------------------- Vérification

    /// <summary>
    /// Les conversions attendues, identiques pour la migration et pour la
    /// restauration d'une ancienne sauvegarde.
    /// </summary>
    private static async Task VerifierLaConversionAsync(CeramiProDbContext contexte)
    {
        // Deux rôles au lieu de quatre : seul l'administrateur le reste.
        var comptes = await contexte.Users.AsNoTracking().OrderBy(u => u.Id).ToListAsync();
        comptes.Select(u => (u.UserName, u.Role)).Should().Equal(
            ("admin", RoleUtilisateur.Administrateur),
            ("karim", RoleUtilisateur.Employe),
            ("sara", RoleUtilisateur.Employe));
        comptes[0].PasswordHash.Should().Be("empreinte-admin", "le mot de passe ne change pas");

        // Le stock des variantes rejoint celui du produit : 3 + 4 + 1,5.
        var produits = await contexte.Products.AsNoTracking().OrderBy(p => p.Id).ToListAsync();
        produits.Select(p => (p.Name, p.CurrentStock)).Should().Equal(
            ("Vase Kabyle", 10m), ("Plat Tlemcen", 8.5m));
        produits[0].ProductionCost.Should().Be(1200.5m);

        // Les mouvements de matières disparaissent ; ceux des produits restent,
        // avec leur nouveau type et l'ancien noté en clair.
        var mouvements = await contexte.InventoryTransactions.AsNoTracking().OrderBy(m => m.Id).ToListAsync();
        mouvements.Select(m => m.Id).Should().Equal(3, 4, 5, 6);

        mouvements[0].TransactionType.Should().Be(InventoryTransactionType.Entree);
        mouvements[0].Notes.Should().Be("Fournée de mai — Ancien type : entrée de production");

        mouvements[1].TransactionType.Should().Be(InventoryTransactionType.Regularisation);
        mouvements[1].CustomOrderId.Should().BeNull("un ancien ajustement n'est pas une commande");

        mouvements[2].TransactionType.Should().Be(InventoryTransactionType.Regularisation);
        mouvements[2].Notes.Should().Be("Ancien type : transfert");

        mouvements[3].TransactionType.Should().Be(InventoryTransactionType.Vente);
        mouvements[3].ProductId.Should().Be(2, "la vente d'une variante revient à son produit");
        mouvements[3].SaleId.Should().Be(1);
        mouvements[3].Notes.Should().Be("Variante : Bleu");

        // Les lignes de vente et la vente elle-même sont intactes.
        var vente = await contexte.Sales.AsNoTracking().Include(v => v.Items).SingleAsync();
        vente.TotalAmount.Should().Be(6000m);
        vente.Items.Should().HaveCount(2);

        // Quatre étapes de commande au lieu de dix ; l'acompte reste acquis.
        var commandes = await contexte.CustomOrders.AsNoTracking().OrderBy(c => c.Id).ToListAsync();
        commandes.Select(c => (c.OrderNumber, c.Status)).Should().Equal(
            ("CMD-2026-0001", CustomOrderStatus.Recue),
            ("CMD-2026-0002", CustomOrderStatus.EnFabrication));
        commandes[0].Notes.Should().Be("Ancienne étape : conception");
        commandes[1].Notes.Should().Be("Bordure dorée — Ancienne étape : décoration");
        commandes[0].PaidAmount.Should().Be(4000m);
        commandes[0].Channel.Should().Be(CanalCommande.Atelier, "le canal d'une ancienne commande est inconnu");

        // Le paiement enregistré sur la facture garde le lien vers la vente ;
        // le décaissement égaré est masqué, pas effacé.
        var paiement = await contexte.Payments.AsNoTracking().SingleAsync(p => p.SaleId != null);
        paiement.PaymentNumber.Should().Be("PAI-2026-0001");
        paiement.SaleId.Should().Be(1);
        paiement.CustomerId.Should().Be(1);

        var masque = await contexte.Payments.IgnoreQueryFilters().AsNoTracking().SingleAsync(p => p.Id == 2);
        masque.IsDeleted.Should().BeTrue();
        masque.Amount.Should().Be(900m);

        // Numérotation : les nouveaux préfixes valent ceux déjà utilisés.
        var parametres = await contexte.BusinessSettings.AsNoTracking().SingleAsync();
        (parametres.CustomerPrefix, parametres.ProductPrefix, parametres.ExpensePrefix)
            .Should().Be(("CLI", "PRD", "DEP"));
        (parametres.SalePrefix, parametres.CustomOrderPrefix, parametres.PaymentPrefix)
            .Should().Be(("VTE", "CMD", "PAI"));
        parametres.ReceiptFooter.Should().Be("Merci et à bientôt.");
        parametres.WorkshopName.Should().Be("Atelier Tassili");
    }

    // ------------------------------------------------------------- Préparation

    private ServiceProvider ConstruireServices()
    {
        var chaine = new NpgsqlConnectionStringBuilder(PostgresDisponible.ChaineConnexion)
        {
            Database = NomBase
        }.ConnectionString;

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CeramiProDB"] = chaine,
                ["Atelier:FuseauHoraire"] = "Africa/Algiers",
                ["Sauvegarde:Dossier"] = _dossier
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AjouterApplication();
        services.AjouterInfrastructure(configuration);

        return services.BuildServiceProvider();
    }

    /// <summary>Base neuve à l'ancien schéma, remplie de l'atelier fictif.</summary>
    private static async Task PreparerAncienneBaseAsync(CeramiProDbContext contexte)
    {
        await contexte.Database.EnsureDeletedAsync();
        await contexte.GetService<IMigrator>().MigrateAsync(AncienneMigration);

        var script = await File.ReadAllTextAsync(
            Path.Combine(AppContext.BaseDirectory, "Donnees", "ancienne-version.sql"));

        await ExecuterAsync(contexte, script);
    }

    /// <summary>
    /// Écrit une sauvegarde au format de l'ancienne version : un fichier CSV
    /// par table du schéma, y compris les tables vides.
    /// </summary>
    private static async Task EcrireSauvegardeAncienneVersionAsync(CeramiProDbContext contexte, string chemin)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(chemin)!);

        var tables = await ListerAsync(contexte, """
            SELECT table_name FROM information_schema.tables
            WHERE table_schema = 'public' AND table_name <> '__EFMigrationsHistory'
            ORDER BY table_name
            """);

        tables.Should().HaveCount(49, "l'ancien schéma comptait 49 tables");

        await using var fichier = File.Create(chemin);
        using var archive = new ZipArchive(fichier, ZipArchiveMode.Create);

        var connexion = contexte.Database.GetDbConnection();
        await connexion.OpenAsync();

        try
        {
            foreach (var table in tables)
            {
                var entree = archive.CreateEntry($"donnees/{table}.csv");
                await using var flux = entree.Open();
                await using var ecriture = new StreamWriter(flux, new UTF8Encoding(true));

                await using var commande = connexion.CreateCommand();
                commande.CommandText = $"SELECT * FROM \"{table}\"";
                await using var lecteur = await commande.ExecuteReaderAsync();

                await ecriture.WriteLineAsync(string.Join(';',
                    Enumerable.Range(0, lecteur.FieldCount).Select(i => lecteur.GetName(i))));

                while (await lecteur.ReadAsync())
                {
                    await ecriture.WriteLineAsync(string.Join(';',
                        Enumerable.Range(0, lecteur.FieldCount).Select(i => ValeurCsv(lecteur, i))));
                }
            }
        }
        finally
        {
            await connexion.CloseAsync();
        }
    }

    /// <summary>Même écriture que l'ancienne version du service de sauvegarde.</summary>
    private static string ValeurCsv(DbDataReader lecteur, int colonne)
    {
        if (lecteur.IsDBNull(colonne))
        {
            return string.Empty;
        }

        var texte = lecteur.GetValue(colonne) switch
        {
            string s when s.Length == 0 => null,
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            decimal nombre => nombre.ToString(CultureInfo.InvariantCulture),
            bool booleen => booleen ? "true" : "false",
            var valeur => valeur.ToString()
        };

        if (texte is null)
        {
            return "\"\"";
        }

        return texte.Contains(';') || texte.Contains('"') || texte.Contains('\n')
            ? $"\"{texte.Replace("\"", "\"\"")}\""
            : texte;
    }

    private static Task ExecuterAsync(CeramiProDbContext contexte, string sql)
        => contexte.Database.ExecuteSqlRawAsync(sql);

    private static async Task<long> CompterAsync(CeramiProDbContext contexte, string sql)
        => (await ListerAsync(contexte, sql)).Select(long.Parse).Single();

    private static async Task<List<string>> ListerAsync(CeramiProDbContext contexte, string sql)
    {
        var connexion = contexte.Database.GetDbConnection();
        var ouverte = connexion.State == System.Data.ConnectionState.Open;

        if (!ouverte)
        {
            await connexion.OpenAsync();
        }

        try
        {
            await using var commande = connexion.CreateCommand();
            commande.CommandText = sql;
            await using var lecteur = await commande.ExecuteReaderAsync();

            var valeurs = new List<string>();

            while (await lecteur.ReadAsync())
            {
                valeurs.Add(Convert.ToString(lecteur.GetValue(0), CultureInfo.InvariantCulture)!);
            }

            return valeurs;
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
