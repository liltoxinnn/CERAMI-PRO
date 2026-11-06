using CeramiPro.Application.Services;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Tests;

public class ConnexionVueModeleTests : IDisposable
{
    private readonly Infrastructure.Data.CeramiProDbContext _contexte;
    private readonly UtilisateurCourantFactice _session = new();
    private readonly ConnexionVueModele _vue;

    public ConnexionVueModeleTests()
    {
        _contexte = ContexteTest.Creer(_session, new HorlogeFactice());
        _session.Fermer();

        var auth = new AuthService(
            _contexte, new PasswordHasherService(), new HorlogeFactice(), new AuditFactice(),
            _session, LoggerFactory.Create(b => { }).CreateLogger<AuthService>());

        _vue = new ConnexionVueModele(auth);
    }

    [Fact]
    public async Task Une_connexion_valide_signale_la_reussite_et_le_profil()
    {
        _vue.NomUtilisateur = "admin";
        _vue.MotDePasse = ContexteTest.MotDePasseAdministrateur;

        await _vue.ConnecterCommand.ExecuteAsync(null);

        _vue.ConnexionReussie.Should().BeTrue();
        _vue.Profil!.NomUtilisateur.Should().Be("admin");
        _vue.MessageErreur.Should().BeNull();
    }

    [Fact]
    public async Task Le_mot_de_passe_ne_reste_pas_en_memoire_apres_la_connexion()
    {
        _vue.NomUtilisateur = "admin";
        _vue.MotDePasse = ContexteTest.MotDePasseAdministrateur;

        await _vue.ConnecterCommand.ExecuteAsync(null);

        _vue.MotDePasse.Should().BeEmpty();
    }

    [Fact]
    public async Task Le_mot_de_passe_est_efface_meme_apres_un_echec()
    {
        _vue.NomUtilisateur = "admin";
        _vue.MotDePasse = "MauvaisMotDePasse@2026";

        await _vue.ConnecterCommand.ExecuteAsync(null);

        _vue.MotDePasse.Should().BeEmpty();
        _vue.ConnexionReussie.Should().BeFalse();
    }

    [Fact]
    public async Task Un_echec_affiche_le_message_du_service_sans_detail_technique()
    {
        _vue.NomUtilisateur = "admin";
        _vue.MotDePasse = "MauvaisMotDePasse@2026";

        await _vue.ConnecterCommand.ExecuteAsync(null);

        _vue.MessageErreur.Should().Be(AuthService.MessageIdentifiantsInvalides);
        _vue.MessageErreur.Should().NotContain("Exception");
    }

    [Fact]
    public async Task Des_champs_vides_sont_signales_sans_appeler_le_service()
    {
        await _vue.ConnecterCommand.ExecuteAsync(null);

        _vue.MessageErreur.Should().Be("Saisissez votre nom d'utilisateur et votre mot de passe.");
        _vue.ConnexionReussie.Should().BeFalse();
    }

    [Fact]
    public async Task Le_profil_porte_le_role_de_la_personne()
    {
        _vue.NomUtilisateur = "admin";
        _vue.MotDePasse = ContexteTest.MotDePasseAdministrateur;

        await _vue.ConnecterCommand.ExecuteAsync(null);

        _session.EstAdministrateur.Should().BeTrue();
        _session.NomRole.Should().Be("Administrateur");
    }

    [Fact]
    public void L_ecran_de_connexion_est_en_francais()
    {
        _vue.LibelleConnexion.Should().Be("Se connecter");
        _vue.LibelleMotDePasse.Should().Be("Mot de passe");
        _vue.LibelleNomUtilisateur.Should().Be("Nom d'utilisateur");
    }

    public void Dispose() => _contexte.Dispose();
}
