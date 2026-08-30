using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Application.Services;
using CeramicWorkshop.Infrastructure.Authentication;
using CeramicWorkshop.Infrastructure.Data;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace CeramicWorkshop.UnitTests.Services;

public class AuthServiceTests : IDisposable
{
    private readonly ApplicationDbContext _contexte;
    private readonly HorlogeFactice _horloge = new();
    private readonly AuditFactice _audit = new();
    private readonly UtilisateurCourantFactice _utilisateurCourant = new();
    private readonly AuthService _service;

    public AuthServiceTests()
    {
        _contexte = ContexteTest.Creer(_utilisateurCourant, _horloge);
        _service = new AuthService(
            _contexte,
            new PasswordHasherService(),
            new JetonsFactices(),
            _utilisateurCourant,
            _horloge,
            _audit,
            NullLogger<AuthService>.Instance);
    }

    [Fact]
    public async Task La_connexion_reussit_avec_les_bons_identifiants()
    {
        var resultat = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        resultat.Succes.Should().BeTrue();
        resultat.Valeur!.Utilisateur.NomUtilisateur.Should().Be("admin");
        resultat.Valeur.Utilisateur.RoleCode.Should().Be("administrateur");
        resultat.Valeur.Utilisateur.Droits.Should().NotBeEmpty();
        resultat.Valeur.JetonRenouvellement.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task La_derniere_connexion_est_enregistree()
    {
        await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        var utilisateur = await _contexte.Users.FirstAsync();
        utilisateur.LastLoginAt.Should().Be(_horloge.UtcNow);
    }

    [Fact]
    public async Task Un_mot_de_passe_incorrect_est_refuse_sans_reveler_le_compte()
    {
        var resultat = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = "mauvais"
        });

        var inconnu = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "personne",
            MotDePasse = "mauvais"
        });

        resultat.Succes.Should().BeFalse();
        inconnu.Succes.Should().BeFalse();
        resultat.Message.Should().Be(inconnu.Message);
    }

    [Fact]
    public async Task Le_compte_est_bloque_apres_plusieurs_echecs()
    {
        for (var tentative = 0; tentative < AuthService.TentativesAvantBlocage; tentative++)
        {
            await _service.ConnexionAsync(new ConnexionRequete { NomUtilisateur = "admin", MotDePasse = "mauvais" });
        }

        var resultat = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        resultat.Succes.Should().BeFalse();
        resultat.Message.Should().Contain("bloqué");
    }

    [Fact]
    public async Task Le_blocage_est_leve_apres_le_delai()
    {
        for (var tentative = 0; tentative < AuthService.TentativesAvantBlocage; tentative++)
        {
            await _service.ConnexionAsync(new ConnexionRequete { NomUtilisateur = "admin", MotDePasse = "mauvais" });
        }

        _horloge.Avancer(TimeSpan.FromMinutes(AuthService.DureeBlocageMinutes + 1));

        var resultat = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        resultat.Succes.Should().BeTrue();
    }

    [Fact]
    public async Task Un_compte_desactive_ne_peut_pas_se_connecter()
    {
        var utilisateur = await _contexte.Users.FirstAsync();
        utilisateur.IsActive = false;
        await _contexte.SaveChangesAsync();

        var resultat = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        resultat.Succes.Should().BeFalse();
        resultat.Message.Should().Contain("désactivé");
    }

    [Fact]
    public async Task Le_jeton_de_renouvellement_permet_d_obtenir_un_nouveau_jeton()
    {
        var connexion = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        var renouvellement = await _service.RenouvelerAsync(new RenouvellementRequete
        {
            JetonRenouvellement = connexion.Valeur!.JetonRenouvellement
        });

        renouvellement.Succes.Should().BeTrue();
        renouvellement.Valeur!.JetonRenouvellement.Should().NotBe(connexion.Valeur.JetonRenouvellement);
    }

    [Fact]
    public async Task Un_jeton_de_renouvellement_expire_est_refuse()
    {
        var connexion = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        _horloge.Avancer(TimeSpan.FromDays(8));

        var renouvellement = await _service.RenouvelerAsync(new RenouvellementRequete
        {
            JetonRenouvellement = connexion.Valeur!.JetonRenouvellement
        });

        renouvellement.Succes.Should().BeFalse();
    }

    [Fact]
    public async Task Le_changement_de_mot_de_passe_verifie_l_ancien()
    {
        var utilisateur = await _contexte.Users.FirstAsync();
        _utilisateurCourant.UserId = utilisateur.Id;

        var resultat = await _service.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
        {
            MotDePasseActuel = "mauvais",
            NouveauMotDePasse = "Nouveau@2026",
            ConfirmationMotDePasse = "Nouveau@2026"
        });

        resultat.Succes.Should().BeFalse();
        resultat.Message.Should().Contain("actuel");
    }

    [Fact]
    public async Task Le_changement_de_mot_de_passe_exige_une_confirmation_identique()
    {
        var utilisateur = await _contexte.Users.FirstAsync();
        _utilisateurCourant.UserId = utilisateur.Id;

        var resultat = await _service.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
        {
            MotDePasseActuel = ContexteTest.MotDePasseAdministrateur,
            NouveauMotDePasse = "Nouveau@2026",
            ConfirmationMotDePasse = "Different@2026"
        });

        resultat.Succes.Should().BeFalse();
    }

    [Fact]
    public async Task Le_nouveau_mot_de_passe_devient_actif()
    {
        var utilisateur = await _contexte.Users.FirstAsync();
        _utilisateurCourant.UserId = utilisateur.Id;

        await _service.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
        {
            MotDePasseActuel = ContexteTest.MotDePasseAdministrateur,
            NouveauMotDePasse = "Nouveau@2026",
            ConfirmationMotDePasse = "Nouveau@2026"
        });

        var connexion = await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = "Nouveau@2026"
        });

        connexion.Succes.Should().BeTrue();
    }

    [Fact]
    public async Task Les_connexions_sont_journalisees()
    {
        await _service.ConnexionAsync(new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = ContexteTest.MotDePasseAdministrateur
        });

        _audit.Traces.Should().ContainSingle(t => t.Action == CeramicWorkshop.Domain.Enums.AuditAction.Connexion);
    }

    [Fact]
    public async Task Les_echecs_de_connexion_sont_journalises()
    {
        await _service.ConnexionAsync(new ConnexionRequete { NomUtilisateur = "admin", MotDePasse = "mauvais" });

        _audit.Traces.Should().ContainSingle(t => t.Action == CeramicWorkshop.Domain.Enums.AuditAction.EchecConnexion);
    }

    public void Dispose() => _contexte.Dispose();
}
