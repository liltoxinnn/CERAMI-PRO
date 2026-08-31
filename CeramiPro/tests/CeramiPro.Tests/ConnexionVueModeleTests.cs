using CeramiPro.Application.Localisation;
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
    private readonly ServiceLangue _langue = new();
    private readonly ConnexionVueModele _vue;

    public ConnexionVueModeleTests()
    {
        _contexte = ContexteTest.Creer(_session, new HorlogeFactice());
        _session.Fermer();

        var auth = new AuthService(
            _contexte, new PasswordHasherService(), new HorlogeFactice(), new AuditFactice(),
            _session, LoggerFactory.Create(b => { }).CreateLogger<AuthService>());

        _vue = new ConnexionVueModele(auth, _langue);
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

        _vue.MessageErreur.Should().Be("Champ obligatoire");
        _vue.ConnexionReussie.Should().BeFalse();
    }

    [Fact]
    public void L_ecran_de_connexion_se_traduit_en_arabe()
    {
        _vue.LibelleConnexion.Should().Be("Se connecter");

        _langue.Changer(Langue.Arabe);

        _vue.LibelleConnexion.Should().Be("تسجيل الدخول");
        _vue.LibelleMotDePasse.Should().Be("كلمة المرور");
        _vue.Sens.Should().Be(SensEcriture.DroiteAGauche);
    }

    public void Dispose() => _contexte.Dispose();
}
