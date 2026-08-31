using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Infrastructure.Services;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Tests;

/// <summary>
/// Vérifie que le socle technique tient : les services se construisent, le
/// contexte se connecte à PostgreSQL et sait créer la base.
/// </summary>
public class InjectionDependancesTests
{
    private static IConfiguration Configuration(string? chaine = null)
        => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:CeramiProDB"] =
                chaine ?? "Host=localhost;Port=5432;Database=CeramiProDB;Username=postgres;Password=x",
            ["Atelier:FuseauHoraire"] = "Africa/Algiers"
        }).Build();

    private static ServiceProvider Construire(string? chaine = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AjouterInfrastructure(Configuration(chaine));
        return services.BuildServiceProvider();
    }

    [Fact]
    public void Tous_les_services_techniques_se_construisent()
    {
        using var fournisseur = Construire();

        fournisseur.GetRequiredService<IServiceDateHeure>().Should().NotBeNull();
        fournisseur.GetRequiredService<IUtilisateurCourant>().Should().NotBeNull();
        fournisseur.GetRequiredService<CeramiProDbContext>().Should().NotBeNull();
    }

    [Fact]
    public void Une_connexion_absente_est_signalee_clairement()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        var configurationVide = new ConfigurationBuilder().Build();

        var action = () => services.AjouterInfrastructure(configurationVide);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*ConnectionStrings:CeramiProDB*");
    }

    [Fact]
    public void La_session_est_fermee_tant_que_personne_ne_s_est_connecte()
    {
        using var fournisseur = Construire();
        var session = fournisseur.GetRequiredService<IUtilisateurCourant>();

        session.EstConnecte.Should().BeFalse();
        session.PossedeDroit("produits.consulter").Should().BeFalse();
    }

    [Fact]
    public void Ouvrir_puis_fermer_une_session_accorde_puis_retire_les_droits()
    {
        using var fournisseur = Construire();
        var session = fournisseur.GetRequiredService<UtilisateurCourant>();

        session.Ouvrir(1, "admin", "Administrateur", "administrateur", "Administrateur",
            new[] { "produits.consulter" });

        session.EstConnecte.Should().BeTrue();
        session.PossedeDroit("produits.consulter").Should().BeTrue();
        session.PossedeDroit("ventes.creer").Should().BeFalse();

        session.Fermer();

        session.EstConnecte.Should().BeFalse();
        session.PossedeDroit("produits.consulter").Should().BeFalse();
    }
}

public class HorlogeTests
{
    private static ServiceDateHeure Horloge(string fuseau)
        => new(LoggerFactory.Create(b => { }).CreateLogger<ServiceDateHeure>(), fuseau);

    [Fact]
    public void L_heure_de_l_atelier_est_celle_d_Alger()
    {
        var horloge = Horloge("Africa/Algiers");

        // Alger est à UTC+1 toute l'année.
        var utc = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

        horloge.VersHeureAtelier(utc).Hour.Should().Be(13);
    }

    [Fact]
    public void La_conversion_dans_les_deux_sens_retombe_sur_la_meme_heure()
    {
        var horloge = Horloge("Africa/Algiers");
        var utc = new DateTime(2026, 8, 31, 12, 0, 0, DateTimeKind.Utc);

        horloge.VersUtc(horloge.VersHeureAtelier(utc)).Should().Be(utc);
    }

    [Fact]
    public void Un_fuseau_inconnu_n_empeche_pas_le_logiciel_de_fonctionner()
    {
        var horloge = Horloge("Fuseau/Inexistant");

        horloge.MaintenantAtelier.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromMinutes(1));
    }
}

public class ContexteBaseDeDonneesTests
{
    [PostgresFact]
    public async Task La_base_CeramiProDB_peut_etre_creee_et_supprimee()
    {
        var options = new DbContextOptionsBuilder<CeramiProDbContext>()
            .UseNpgsql(PostgresDisponible.ChaineConnexion)
            .Options;

        await using var contexte = new CeramiProDbContext(options);

        await contexte.Database.EnsureDeletedAsync();
        (await contexte.Database.EnsureCreatedAsync()).Should().BeTrue();
        (await contexte.Database.CanConnectAsync()).Should().BeTrue();

        await contexte.Database.EnsureDeletedAsync();
    }

    [PostgresFact]
    public async Task Le_contexte_annonce_le_moteur_PostgreSQL()
    {
        var options = new DbContextOptionsBuilder<CeramiProDbContext>()
            .UseNpgsql(PostgresDisponible.ChaineConnexion)
            .Options;

        await using var contexte = new CeramiProDbContext(options);

        contexte.Database.ProviderName.Should().Contain("Npgsql");
    }
}
