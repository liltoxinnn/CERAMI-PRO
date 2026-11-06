using CeramiPro.Application;
using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Infrastructure.Data.Seed;
using CeramiPro.Presentation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CeramiPro.Tests;

/// <summary>
/// Reproduit le démarrage réel du logiciel sur un poste neuf : migrations
/// appliquées à une base vierge, amorçage, puis ouverture de chaque écran.
///
/// C'est le chemin que suit l'atelier le premier jour. Les tests métier
/// travaillent sur une base en mémoire, qui ne dit rien des migrations, des
/// clés étrangères ni des index : seul un vrai PostgreSQL les met à
/// l'épreuve.
/// </summary>
[Collection(CollectionPostgres.Nom)]
public class DemarrageReelTests
{
    private const string NomBase = "CeramiProDB_Demarrage";

    private static string ChaineConnexion()
        => new NpgsqlConnectionStringBuilder(PostgresDisponible.ChaineConnexion)
        {
            Database = NomBase
        }.ConnectionString;

    /// <summary>
    /// Construit les services comme le fait l'application Windows, les
    /// fenêtres en moins : celles-ci n'ont pas de place dans un test.
    /// </summary>
    private static ServiceProvider ConstruireServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CeramiProDB"] = ChaineConnexion(),
                ["Atelier:FuseauHoraire"] = "Africa/Algiers"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AjouterApplication();
        services.AjouterInfrastructure(configuration);

        services.AddSingleton<IServiceDialogue, DialogueFactice>();
        services.AddSingleton<IServiceFormulaire, FormulaireFactice>();
        services.AddSingleton<IServiceFichier, FichierFactice>();
        services.AddSingleton<IServiceFenetres, FenetresFactice>();
        services.AddSingleton<IServiceCompte, CompteFactice>();

        services.AjouterPresentation();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = false
        });
    }

    [PostgresFact]
    public async Task Les_migrations_s_appliquent_sur_une_base_vierge_puis_l_amorcage_reussit()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            // Exactement ce que fait l'application au démarrage.
            await contexte.Database.MigrateAsync();

            var appliquees = await contexte.Database.GetAppliedMigrationsAsync();
            appliquees.Should().NotBeEmpty("la migration initiale doit s'appliquer");

            (await contexte.Database.GetPendingMigrationsAsync()).Should().BeEmpty(
                "aucune migration ne doit rester en attente après la mise à jour");

            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();

            // L'amorçage rejoué ne doit rien dupliquer : le logiciel l'exécute
            // à chaque démarrage.
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();

            (await contexte.Users.CountAsync()).Should().Be(1);
            (await contexte.PaymentMethods.CountAsync()).Should().BeGreaterThan(0);
            (await contexte.ProductCategories.CountAsync()).Should().BeGreaterThan(0);
            (await contexte.ExpenseCategories.CountAsync()).Should().BeGreaterThan(0);
            (await contexte.BusinessSettings.CountAsync()).Should().Be(1);
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Les_dix_neuf_tables_existent_apres_migration()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();

            await using var connexion = new NpgsqlConnection(ChaineConnexion());
            await connexion.OpenAsync();

            await using var commande = connexion.CreateCommand();
            commande.CommandText =
                "select count(*) from information_schema.tables " +
                "where table_schema = 'public' and table_type = 'BASE TABLE'";

            var tables = Convert.ToInt32(await commande.ExecuteScalarAsync());

            // Les 19 tables du métier, plus celle de l'historique des migrations.
            tables.Should().Be(20);

            // Sur une installation neuve, il n'y a rien à archiver.
            commande.CommandText =
                "select count(*) from information_schema.tables where table_schema = 'archives'";
            Convert.ToInt32(await commande.ExecuteScalarAsync()).Should().Be(0);
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task L_administrateur_amorce_peut_se_connecter_et_doit_changer_son_mot_de_passe()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();

            var auth = fournisseur.GetRequiredService<IAuthService>();

            var reponse = await auth.ConnecterAsync(new Application.DTOs.Auth.ConnexionRequete
            {
                NomUtilisateur = "admin",
                MotDePasse = DatabaseSeeder.MotDePasseAdministrateurParDefaut
            });

            reponse.Utilisateur.NomUtilisateur.Should().Be("admin");
            reponse.Utilisateur.Role.Should().Be(Domain.Enums.RoleUtilisateur.Administrateur);

            // Le mot de passe livré avec le logiciel ne doit pas rester en usage.
            reponse.Utilisateur.DoitChangerMotDePasse.Should().BeTrue();

        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Chaque_ecran_du_menu_s_ouvre_sur_une_base_reelle()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();

            var session = fournisseur.GetRequiredService<ISessionAtelier>();
            session.Ouvrir(1, "admin", "Administrateur", Domain.Enums.RoleUtilisateur.Administrateur);

            var navigation = fournisseur.GetRequiredService<IServiceNavigation>();

            var destinations = CatalogueNavigationTests
                .Aplatir(CatalogueNavigation.Construire())
                .Where(e => e.Destination is not null)
                .Select(e => e.Destination!)
                .Distinct()
                .ToList();

            var echecs = new List<string>();

            foreach (var destination in destinations)
            {
                try
                {
                    navigation.Naviguer(destination);

                    // La navigation lance le chargement sans l'attendre :
                    // ici, on l'attend pour voir ce qu'il donne.
                    await navigation.ChargementCourant;

                    if (navigation.VueCourante!.MessageErreur is { } message)
                    {
                        echecs.Add($"{destination.Name} : {message}");
                    }
                }
                catch (Exception erreur)
                {
                    echecs.Add($"{destination.Name} : {erreur.Message}");
                }
            }

            echecs.Should().BeEmpty(
                "ces écrans échouent à s'ouvrir sur une base réelle — "
                + string.Join(" | ", echecs));
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Une_vente_dont_le_reglement_echoue_est_annulee_en_entier()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();
            fournisseur.GetRequiredService<ISessionAtelier>()
                .Ouvrir(1, "admin", "Administrateur", Domain.Enums.RoleUtilisateur.Administrateur);

            var produits = fournisseur.GetRequiredService<IProduitService>();
            var categorie = await contexte.ProductCategories.Select(c => c.Id).FirstAsync();
            var produit = await produits.CreerAsync(new Application.DTOs.Catalogue.ProduitRequete
            {
                Nom = "Vase", CategorieId = categorie, PrixVente = 3000m, StockInitial = 5m
            });

            // La vente et sa sortie de stock sont écrites, puis le règlement
            // échoue : un mode de règlement qui n'existe pas.
            var ventes = fournisseur.GetRequiredService<IVenteService>();
            var action = () => ventes.EnregistrerAsync(new Application.DTOs.Commercial.VenteRequete
            {
                MontantPaye = 3000m,
                ModeReglementId = 999,
                Lignes = new() { new() { ProduitId = produit.Id, Quantite = 2m } }
            });

            await action.Should().ThrowAsync<Application.Common.RegleMetierException>();

            // Rien ne doit en rester : ni vente, ni stock diminué.
            await using var verification = new CeramiProDbContext(
                new DbContextOptionsBuilder<CeramiProDbContext>().UseNpgsql(ChaineConnexion()).Options);

            (await verification.Sales.IgnoreQueryFilters().CountAsync()).Should().Be(0);
            (await verification.Products.SingleAsync(p => p.Id == produit.Id)).CurrentStock.Should().Be(5m);
            (await verification.InventoryTransactions.CountAsync(t => t.TransactionType == Domain.Enums.InventoryTransactionType.Vente))
                .Should().Be(0);

            // Et le logiciel continue normalement : la vente suivante passe.
            var modeId = await contexte.PaymentMethods.Select(m => m.Id).FirstAsync();
            var vente = await ventes.EnregistrerAsync(new Application.DTOs.Commercial.VenteRequete
            {
                MontantPaye = 3000m,
                ModeReglementId = modeId,
                Lignes = new() { new() { ProduitId = produit.Id, Quantite = 1m } }
            });

            vente.Reste.Should().Be(0m);
            (await verification.Products.AsNoTracking().SingleAsync(p => p.Id == produit.Id)).CurrentStock.Should().Be(4m);
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }
}
