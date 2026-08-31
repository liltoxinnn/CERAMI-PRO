using CeramiPro.Domain.Common;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Infrastructure.Data.Seed;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Tests;

/// <summary>
/// Un mot de passe haché ne se retrouve pas. Sans porte de secours, un oubli
/// rendrait le logiciel définitivement inutilisable pour l'atelier.
/// </summary>
public class ReinitialisationAdministrateurTests
{
    private const string NouveauMotDePasse = "Secours@2026";

    private static DatabaseSeeder Semeur(
        Infrastructure.Data.CeramiProDbContext contexte, params (string Cle, string Valeur)[] reglages)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(reglages.ToDictionary(r => r.Cle, r => (string?)r.Valeur))
            .Build();

        return new DatabaseSeeder(
            contexte, new PasswordHasherService(), configuration,
            LoggerFactory.Create(b => { }).CreateLogger<DatabaseSeeder>());
    }

    [Fact]
    public async Task Sans_demande_le_mot_de_passe_reste_inchange()
    {
        using var contexte = ContexteTest.Creer();
        var hachage = new PasswordHasherService();

        await Semeur(contexte).ExecuterAsync();

        var admin = await contexte.Users.FirstAsync(u => u.UserName == "admin");
        hachage.Verifier(ContexteTest.MotDePasseAdministrateur, admin.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task La_demande_redonne_le_mot_de_passe_configure()
    {
        using var contexte = ContexteTest.Creer();
        var hachage = new PasswordHasherService();

        await Semeur(contexte,
            (DatabaseSeeder.CleReinitialisation, "true"),
            (DatabaseSeeder.CleMotDePasseInitial, NouveauMotDePasse)).ExecuterAsync();

        var admin = await contexte.Users.FirstAsync(u => u.UserName == "admin");

        hachage.Verifier(NouveauMotDePasse, admin.PasswordHash).Should().BeTrue();
        hachage.Verifier(ContexteTest.MotDePasseAdministrateur, admin.PasswordHash).Should().BeFalse();
    }

    [Fact]
    public async Task Sans_mot_de_passe_configure_la_valeur_par_defaut_est_appliquee()
    {
        using var contexte = ContexteTest.Creer();

        await Semeur(contexte, (DatabaseSeeder.CleReinitialisation, "true")).ExecuterAsync();

        var admin = await contexte.Users.FirstAsync(u => u.UserName == "admin");

        new PasswordHasherService()
            .Verifier(DatabaseSeeder.MotDePasseAdministrateurParDefaut, admin.PasswordHash)
            .Should().BeTrue();
    }

    [Fact]
    public async Task La_reinitialisation_debloque_un_compte_verrouille()
    {
        using var contexte = ContexteTest.Creer();

        var admin = await contexte.Users.FirstAsync(u => u.UserName == "admin");
        admin.FailedLoginAttempts = 5;
        admin.LockedUntil = DateTime.UtcNow.AddHours(1);
        admin.IsActive = false;
        await contexte.SaveChangesAsync();

        await Semeur(contexte,
            (DatabaseSeeder.CleReinitialisation, "true"),
            (DatabaseSeeder.CleMotDePasseInitial, NouveauMotDePasse)).ExecuterAsync();

        var apres = await contexte.Users.FirstAsync(u => u.UserName == "admin");

        apres.LockedUntil.Should().BeNull();
        apres.FailedLoginAttempts.Should().Be(0);
        apres.IsActive.Should().BeTrue();
    }

    [Fact]
    public async Task Le_changement_de_mot_de_passe_est_ensuite_exige()
    {
        using var contexte = ContexteTest.Creer();

        await Semeur(contexte,
            (DatabaseSeeder.CleReinitialisation, "true"),
            (DatabaseSeeder.CleMotDePasseInitial, NouveauMotDePasse)).ExecuterAsync();

        var admin = await contexte.Users.FirstAsync(u => u.UserName == "admin");
        admin.MustChangePassword.Should().BeTrue();
    }

    [Fact]
    public async Task Une_valeur_autre_que_vrai_ne_declenche_rien()
    {
        using var contexte = ContexteTest.Creer();

        await Semeur(contexte,
            (DatabaseSeeder.CleReinitialisation, "peut-être"),
            (DatabaseSeeder.CleMotDePasseInitial, NouveauMotDePasse)).ExecuterAsync();

        var admin = await contexte.Users.FirstAsync(u => u.UserName == "admin");

        new PasswordHasherService()
            .Verifier(ContexteTest.MotDePasseAdministrateur, admin.PasswordHash)
            .Should().BeTrue();
    }

    [Fact]
    public void Le_role_administrateur_conserve_tous_les_droits()
        => PermissionCodes.DroitsParDefaut[RoleCodes.Administrateur]
            .Should().HaveCount(PermissionCodes.Catalogue.Count);
}
