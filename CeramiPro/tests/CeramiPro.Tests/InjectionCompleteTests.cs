using CeramiPro.Application;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Infrastructure;
using CeramiPro.Presentation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Tests;

/// <summary>
/// Vérifie que chaque service déclaré peut réellement être construit.
///
/// Un contrat oublié dans l'injection de dépendances ne se voit ni à la
/// compilation, ni dans les tests unitaires : il ne se manifeste qu'au
/// démarrage, devant l'utilisateur. Ce test le fait échouer ici.
/// </summary>
public class InjectionCompleteTests
{
    private static ServiceProvider Construire()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CeramiProDB"] =
                    "Host=localhost;Port=5432;Database=CeramiProDB;Username=postgres;Password=x",
                ["Atelier:FuseauHoraire"] = "Africa/Algiers"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AjouterApplication();
        services.AjouterInfrastructure(configuration);

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    [Fact]
    public void Le_contrat_du_contexte_de_donnees_est_enregistre()
    {
        using var fournisseur = Construire();
        using var portee = fournisseur.CreateScope();

        portee.ServiceProvider.GetRequiredService<IApplicationDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void Tous_les_services_metier_se_construisent()
    {
        using var fournisseur = Construire();
        using var portee = fournisseur.CreateScope();

        var contrats = typeof(IAuthService).Assembly.GetTypes()
            .Where(t => t.IsInterface
                        && t.Namespace == "CeramiPro.Application.Interfaces"
                        && t.Name.StartsWith('I'))
            .ToList();

        contrats.Should().NotBeEmpty();

        var manquants = new List<string>();

        foreach (var contrat in contrats)
        {
            if (portee.ServiceProvider.GetService(contrat) is null)
            {
                manquants.Add(contrat.Name);
            }
        }

        manquants.Should().BeEmpty(
            "ces services sont déclarés mais introuvables dans l'injection de dépendances : "
            + string.Join(", ", manquants));
    }
}

/// <summary>
/// Vérifie que tout écran atteignable depuis le menu peut réellement être
/// construit.
///
/// Un écran oublié dans l'injection de dépendances ne se voit ni à la
/// compilation, ni dans les tests métier : il n'échoue qu'au clic, devant
/// l'utilisateur. Ce test le fait échouer ici.
/// </summary>
public class InjectionEcransTests
{
    private static ServiceProvider Construire()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CeramiProDB"] =
                    "Host=localhost;Port=5432;Database=CeramiProDB;Username=postgres;Password=x",
                ["Atelier:FuseauHoraire"] = "Africa/Algiers"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AjouterApplication();
        services.AjouterInfrastructure(configuration);

        // Ce que seule l'application Windows sait faire : les tests s'en
        // tiennent à des doubles, la construction des écrans reste la même.
        services.AddSingleton<IServiceLangue, ServiceLangue>();
        services.AddSingleton<IServiceDialogue, DialogueFactice>();
        services.AddSingleton<IServiceFormulaire, FormulaireFactice>();
        services.AddSingleton<IServiceFichier, FichierFactice>();

        services.AjouterPresentation();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true
        });
    }

    [Fact]
    public void Chaque_ecran_du_menu_se_construit()
    {
        using var fournisseur = Construire();
        using var portee = fournisseur.CreateScope();

        var destinations = CatalogueNavigationTests
            .Aplatir(CatalogueNavigation.Construire(new ServiceLangue()))
            .Where(e => e.Destination is not null)
            .Select(e => e.Destination!)
            .Distinct()
            .ToList();

        destinations.Should().HaveCountGreaterThanOrEqualTo(30);

        var manquants = new List<string>();

        foreach (var destination in destinations)
        {
            try
            {
                if (portee.ServiceProvider.GetService(destination) is null)
                {
                    manquants.Add(destination.Name);
                }
            }
            catch (Exception erreur)
            {
                manquants.Add($"{destination.Name} ({erreur.Message})");
            }
        }

        manquants.Should().BeEmpty(
            "ces écrans sont atteignables depuis le menu mais introuvables dans "
            + "l'injection de dépendances : " + string.Join(", ", manquants));
    }

    [Fact]
    public void Chaque_formulaire_demande_par_un_ecran_se_construit()
    {
        using var fournisseur = Construire();
        using var portee = fournisseur.CreateScope();

        var champ = typeof(ListeVueModele<>).GetProperty(
            "TypeFormulaire",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        champ.Should().NotBeNull("l'écran de liste déclare son formulaire par cette propriété");

        var manquants = new List<string>();

        foreach (var type in EcransTests.TypesListes())
        {
            var ecran = portee.ServiceProvider.GetService(type);

            if (ecran is null)
            {
                manquants.Add(type.Name);
                continue;
            }

            var propriete = type.GetProperty(
                "TypeFormulaire",
                System.Reflection.BindingFlags.NonPublic
                | System.Reflection.BindingFlags.Instance
                | System.Reflection.BindingFlags.FlattenHierarchy);

            if (propriete?.GetValue(ecran) is not Type formulaire)
            {
                continue;
            }

            if (portee.ServiceProvider.GetService(formulaire) is not IFormulaire)
            {
                manquants.Add($"{type.Name} → {formulaire.Name}");
            }
        }

        manquants.Should().BeEmpty(
            "ces formulaires sont déclarés par un écran mais introuvables dans "
            + "l'injection de dépendances : " + string.Join(", ", manquants));
    }

    [Fact]
    public void Les_ecrans_qui_ne_sont_pas_dans_le_menu_se_construisent_aussi()
    {
        using var fournisseur = Construire();
        using var portee = fournisseur.CreateScope();

        // La connexion et le tableau de bord ne figurent pas dans le menu
        // latéral, mais l'application les ouvre au démarrage.
        portee.ServiceProvider.GetService<ConnexionVueModele>().Should().NotBeNull();
        portee.ServiceProvider.GetService<TableauDeBordVueModele>().Should().NotBeNull();
        portee.ServiceProvider.GetService<FenetrePrincipaleVueModele>().Should().NotBeNull();
    }
}
