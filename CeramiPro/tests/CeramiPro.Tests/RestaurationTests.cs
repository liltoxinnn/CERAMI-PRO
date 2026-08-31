using CeramiPro.Application;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Infrastructure;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Infrastructure.Data.Seed;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace CeramiPro.Tests;

/// <summary>
/// Sauvegarde puis restauration, sur un vrai PostgreSQL.
///
/// Une sauvegarde ne vaut que par la restauration : l'archive est donc
/// produite, les données détruites, puis remises en place, et l'on vérifie
/// que l'atelier retrouve exactement ce qu'il avait.
/// </summary>
[Collection(CollectionPostgres.Nom)]
public class RestaurationTests : IDisposable
{
    private const string NomBase = "CeramiProDB_Restauration";

    private readonly string _dossier = Path.Combine(
        Path.GetTempPath(), $"ceramipro-sauvegardes-{Guid.NewGuid():N}");

    public void Dispose()
    {
        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    private string ChaineConnexion()
        => new NpgsqlConnectionStringBuilder(PostgresDisponible.ChaineConnexion)
        {
            Database = NomBase
        }.ConnectionString;

    private ServiceProvider ConstruireServices()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CeramiProDB"] = ChaineConnexion(),
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

    /// <summary>Ouvre une session : les services tracent l'auteur des opérations.</summary>
    private static void OuvrirSession(IServiceProvider fournisseur)
        => fournisseur.GetRequiredService<ISessionAtelier>().Ouvrir(
            1, "admin", "Administrateur", RoleCodes.Administrateur, "Administrateur",
            PermissionCodes.Catalogue.Select(d => d.Code));

    [PostgresFact]
    public async Task Une_sauvegarde_restauree_rend_l_atelier_a_l_identique()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();
            OuvrirSession(fournisseur);

            // Un peu de vie dans l'atelier : un client, un produit, une vente.
            var clients = fournisseur.GetRequiredService<IClientService>();
            var produits = fournisseur.GetRequiredService<IProduitService>();
            var ventes = fournisseur.GetRequiredService<IVenteService>();

            var categorie = await fournisseur.GetRequiredService<IReferentielService>()
                .ListerAsync(Application.DTOs.Referentiels.TypeReferentiel.CategorieProduit);

            var client = await clients.CreerAsync(new ClientRequete
            {
                Nom = "Mohamed Benali",
                Telephone = "0550 11 22 33",
                Ville = "Alger"
            });

            var produit = await produits.CreerAsync(new Application.DTOs.Catalogue.ProduitRequete
            {
                Nom = "Vase bleu d'Alger",
                CategorieId = categorie[0].Id,
                PrixVente = 4200m,
                CoutProduction = 1850m,
                StockInitial = 10m
            });

            var vente = await ventes.EnregistrerAsync(new VenteRequete
            {
                ClientId = client.Id,
                Lignes = new List<LigneVenteRequete>
                {
                    new() { ProduitId = produit.Id, Quantite = 3m, PrixUnitaire = 4200m }
                }
            });

            var sauvegardes = fournisseur.GetRequiredService<ISauvegardeService>();
            var archive = await sauvegardes.CreerAsync();

            // L'atelier continue de tourner : ce qui suit sera perdu, et la
            // fiche modifiée retrouvera son état d'avant.
            await clients.CreerAsync(new ClientRequete { Nom = "Client d'après la sauvegarde" });

            await clients.ModifierAsync(client.Id, new ClientRequete
            {
                Nom = "Nom saisi par erreur",
                Telephone = "0000 00 00 00"
            });

            var resultat = await sauvegardes.RestaurerAsync(archive.NomFichier);

            resultat.NombreTables.Should().BeGreaterThan(0);
            resultat.NombreLignes.Should().BeGreaterThan(0);

            // Le contexte a suivi les entités en mémoire : il faut relire.
            contexte.ChangeTracker.Clear();

            var apres = await clients.ListerAsync(new FiltreClientsRequete { TaillePage = 200 });

            apres.Elements.Should().ContainSingle(c => c.Nom == "Mohamed Benali");
            apres.Elements.Should().NotContain(c => c.Nom == "Client d'après la sauvegarde");

            var restaure = apres.Elements.Single(c => c.Nom == "Mohamed Benali");
            restaure.Telephone.Should().Be("0550 11 22 33");
            restaure.Ville.Should().Be("Alger");
            restaure.Email.Should().BeNull("un champ absent reste absent, il ne devient pas vide");

            var venteRestauree = await ventes.ObtenirAsync(vente.Id);
            venteRestauree.Total.Should().Be(12600m);
            venteRestauree.Lignes.Should().ContainSingle();

            var produitRestaure = await produits.ObtenirAsync(produit.Id);
            produitRestaure.StockActuel.Should().Be(7m);
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Apres_restauration_les_nouvelles_fiches_ne_heurtent_pas_les_anciennes()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();
            OuvrirSession(fournisseur);

            var clients = fournisseur.GetRequiredService<IClientService>();

            for (var rang = 1; rang <= 3; rang++)
            {
                await clients.CreerAsync(new ClientRequete { Nom = $"Client {rang}" });
            }

            var sauvegardes = fournisseur.GetRequiredService<ISauvegardeService>();
            var archive = await sauvegardes.CreerAsync();

            await sauvegardes.RestaurerAsync(archive.NomFichier);
            contexte.ChangeTracker.Clear();

            // Le compteur d'identifiants doit repartir après la dernière fiche
            // restaurée, sans quoi la création suivante échouerait.
            var nouveau = await clients.CreerAsync(new ClientRequete { Nom = "Client d'après" });

            nouveau.Id.Should().BeGreaterThan(3);

            var tous = await clients.ListerAsync(new FiltreClientsRequete { TaillePage = 200 });
            tous.Total.Should().Be(4);
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Un_texte_vide_ne_devient_pas_une_valeur_absente()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();
            await fournisseur.GetRequiredService<DatabaseSeeder>().ExecuterAsync();
            OuvrirSession(fournisseur);

            // Les services normalisent les champs laissés blancs : la
            // distinction est donc éprouvée en écrivant directement dans la
            // base, comme pourrait le faire une version future du logiciel.
            contexte.Customers.Add(new Domain.Entities.Customers.Customer
            {
                CustomerNumber = "CLI-2026-9001",
                FullName = "Client aux champs mêlés",
                City = string.Empty,
                Email = null
            });

            await contexte.SaveChangesAsync();
            contexte.ChangeTracker.Clear();

            var sauvegardes = fournisseur.GetRequiredService<ISauvegardeService>();
            var archive = await sauvegardes.CreerAsync();

            await sauvegardes.RestaurerAsync(archive.NomFichier);
            contexte.ChangeTracker.Clear();

            var restaure = await contexte.Customers.AsNoTracking()
                .SingleAsync(c => c.CustomerNumber == "CLI-2026-9001");

            restaure.City.Should().BeEmpty("un texte vide reste un texte vide");
            restaure.Email.Should().BeNull("une valeur absente reste absente");
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Un_fichier_qui_n_est_pas_une_sauvegarde_est_refuse_avec_un_message_clair()
    {
        await using var fournisseur = ConstruireServices();
        var contexte = fournisseur.GetRequiredService<CeramiProDbContext>();

        await contexte.Database.EnsureDeletedAsync();

        try
        {
            await contexte.Database.MigrateAsync();

            Directory.CreateDirectory(_dossier);

            var intrus = Path.Combine(_dossier, "ceramipro-intrus.zip");

            using (var flux = new FileStream(intrus, FileMode.Create))
            using (var archive = new System.IO.Compression.ZipArchive(
                       flux, System.IO.Compression.ZipArchiveMode.Create))
            {
                archive.CreateEntry("photo.jpg");
            }

            var sauvegardes = fournisseur.GetRequiredService<ISauvegardeService>();

            var action = async () => await sauvegardes.RestaurerAsync("ceramipro-intrus.zip");

            (await action.Should().ThrowAsync<Application.Common.RegleMetierException>())
                .WithMessage("*ne contient aucune donnée*");
        }
        finally
        {
            await contexte.Database.EnsureDeletedAsync();
        }
    }

    [PostgresFact]
    public async Task Une_sauvegarde_introuvable_est_signalee()
    {
        await using var fournisseur = ConstruireServices();

        var action = async () => await fournisseur.GetRequiredService<ISauvegardeService>()
            .RestaurerAsync("ceramipro-inexistante.zip");

        await action.Should().ThrowAsync<Application.Common.IntrouvableException>();
    }
}
