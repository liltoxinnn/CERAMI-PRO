using CeramiPro.Application;
using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure;
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
