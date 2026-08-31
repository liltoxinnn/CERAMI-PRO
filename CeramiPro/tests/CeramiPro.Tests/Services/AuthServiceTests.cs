using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Auth;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Tests.Services;

/// <summary>
/// Ouverture de session de l'application de bureau. Ces vérifications portent
/// sur de la sécurité : elles doivent rester lisibles et exhaustives.
/// </summary>
public class AuthServiceTests : IDisposable
{
    private readonly CeramiProDbContext _contexte;
    private readonly UtilisateurCourantFactice _session = new();
    private readonly HorlogeFactice _horloge = new();
    private readonly AuditFactice _audit = new();
    private readonly AuthService _auth;

    public AuthServiceTests()
    {
        _contexte = ContexteTest.Creer(_session, _horloge);
        _session.Fermer();

        _auth = new AuthService(
            _contexte,
            new PasswordHasherService(),
            _horloge,
            _audit,
            _session,
            LoggerFactory.Create(b => { }).CreateLogger<AuthService>());
    }

    private ConnexionRequete Identifiants(string? motDePasse = null) => new()
    {
        NomUtilisateur = "admin",
        MotDePasse = motDePasse ?? ContexteTest.MotDePasseAdministrateur
    };

    [Fact]
    public async Task Une_connexion_valide_ouvre_la_session()
    {
        var reponse = await _auth.ConnecterAsync(Identifiants());

        reponse.Utilisateur.NomUtilisateur.Should().Be("admin");
        reponse.Utilisateur.Droits.Should().NotBeEmpty();

        _session.EstConnecte.Should().BeTrue();
        _session.NomUtilisateur.Should().Be("admin");
    }

    [Fact]
    public async Task Une_connexion_valide_accorde_les_droits_du_role()
    {
        await _auth.ConnecterAsync(Identifiants());

        _session.PossedeDroit(PermissionCodes.ProduitsConsulter).Should().BeTrue();
    }

    [Fact]
    public async Task Un_nom_inconnu_et_un_mot_de_passe_faux_donnent_le_meme_message()
    {
        var nomInconnu = await Refus(new ConnexionRequete
        {
            NomUtilisateur = "inexistant",
            MotDePasse = "PeuImporte@2026"
        });

        var mauvaisMotDePasse = await Refus(Identifiants("MauvaisMotDePasse@2026"));

        // Deux messages différents révéleraient quels comptes existent.
        nomInconnu.Should().Be(mauvaisMotDePasse);
        nomInconnu.Should().Be(AuthService.MessageIdentifiantsInvalides);
    }

    [Fact]
    public async Task Un_echec_de_connexion_ne_laisse_aucune_session_ouverte()
    {
        await Refus(Identifiants("MauvaisMotDePasse@2026"));

        _session.EstConnecte.Should().BeFalse();
    }

    [Fact]
    public async Task Le_compte_se_bloque_apres_cinq_essais_manques()
    {
        for (var essai = 0; essai < AuthService.TentativesAvantBlocage; essai++)
        {
            await Refus(Identifiants("MauvaisMotDePasse@2026"));
        }

        // Le bon mot de passe est désormais refusé lui aussi.
        var message = await Refus(Identifiants());

        message.Should().Contain("bloqué");
        message.Should().Contain("minute");
    }

    [Fact]
    public async Task Le_blocage_se_leve_apres_le_delai()
    {
        for (var essai = 0; essai < AuthService.TentativesAvantBlocage; essai++)
        {
            await Refus(Identifiants("MauvaisMotDePasse@2026"));
        }

        _horloge.Avancer(TimeSpan.FromMinutes(AuthService.DureeBlocageMinutes + 1));

        var reponse = await _auth.ConnecterAsync(Identifiants());

        reponse.Utilisateur.NomUtilisateur.Should().Be("admin");
    }

    [Fact]
    public async Task Une_connexion_reussie_remet_le_compteur_d_essais_a_zero()
    {
        await Refus(Identifiants("MauvaisMotDePasse@2026"));
        await Refus(Identifiants("MauvaisMotDePasse@2026"));

        await _auth.ConnecterAsync(Identifiants());

        var utilisateur = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        utilisateur.FailedLoginAttempts.Should().Be(0);
        utilisateur.LastLoginAt.Should().NotBeNull();
    }

    [Fact]
    public async Task Un_compte_desactive_est_refuse_avec_un_message_explicite()
    {
        var utilisateur = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        utilisateur.IsActive = false;
        await _contexte.SaveChangesAsync();

        var message = await Refus(Identifiants());

        message.Should().Contain("désactivé");
    }

    [Fact]
    public async Task Chaque_tentative_est_inscrite_au_journal()
    {
        await Refus(Identifiants("MauvaisMotDePasse@2026"));
        await _auth.ConnecterAsync(Identifiants());

        _audit.Traces.Should().Contain(t => t.Action == AuditAction.EchecConnexion);
        _audit.Traces.Should().Contain(t => t.Action == AuditAction.Connexion);
    }

    [Fact]
    public async Task La_deconnexion_ferme_la_session_et_retire_les_droits()
    {
        await _auth.ConnecterAsync(Identifiants());

        await _auth.DeconnecterAsync();

        _session.EstConnecte.Should().BeFalse();
        _session.PossedeDroit(PermissionCodes.ProduitsConsulter).Should().BeFalse();
        _audit.Traces.Should().Contain(t => t.Action == AuditAction.Deconnexion);
    }

    [Fact]
    public async Task Changer_son_mot_de_passe_permet_de_se_reconnecter_avec_le_nouveau()
    {
        await _auth.ConnecterAsync(Identifiants());

        await _auth.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
        {
            MotDePasseActuel = ContexteTest.MotDePasseAdministrateur,
            NouveauMotDePasse = "NouveauMotDePasse@2026",
            ConfirmationMotDePasse = "NouveauMotDePasse@2026"
        });

        await _auth.DeconnecterAsync();

        var reponse = await _auth.ConnecterAsync(Identifiants("NouveauMotDePasse@2026"));
        reponse.Utilisateur.DoitChangerMotDePasse.Should().BeFalse();
    }

    [Fact]
    public async Task Un_mot_de_passe_actuel_errone_empeche_le_changement()
    {
        await _auth.ConnecterAsync(Identifiants());

        var action = async () => await _auth.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
        {
            MotDePasseActuel = "PasLeBon@2026",
            NouveauMotDePasse = "NouveauMotDePasse@2026",
            ConfirmationMotDePasse = "NouveauMotDePasse@2026"
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*actuel est incorrect*");
    }

    [Fact]
    public async Task Une_confirmation_differente_empeche_le_changement()
    {
        await _auth.ConnecterAsync(Identifiants());

        var action = async () => await _auth.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
        {
            MotDePasseActuel = ContexteTest.MotDePasseAdministrateur,
            NouveauMotDePasse = "NouveauMotDePasse@2026",
            ConfirmationMotDePasse = "AutreChose@2026"
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*ne correspondent pas*");
    }

    [Fact]
    public async Task Changer_de_mot_de_passe_sans_session_est_refuse()
    {
        var action = async () => await _auth.ChangerMotDePasseAsync(new ChangementMotDePasseRequete());

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*Aucune session*");
    }

    /// <summary>Lance la connexion et renvoie le message de refus.</summary>
    private async Task<string> Refus(ConnexionRequete requete)
    {
        try
        {
            await _auth.ConnecterAsync(requete);
        }
        catch (RegleMetierException erreur)
        {
            return erreur.Message;
        }

        throw new Xunit.Sdk.XunitException("La connexion aurait dû être refusée.");
    }

    public void Dispose() => _contexte.Dispose();
}
