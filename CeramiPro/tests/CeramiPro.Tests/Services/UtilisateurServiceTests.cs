using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Identity;
using CeramiPro.Application.Services;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Authentication;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Tests.Services;

public class UtilisateurServiceTests : IDisposable
{
    private readonly CeramiProDbContext _contexte;
    private readonly UtilisateurCourantFactice _utilisateurCourant = new();
    private readonly AuditFactice _audit = new();
    private readonly UtilisateurService _service;

    public UtilisateurServiceTests()
    {
        _contexte = ContexteTest.Creer(_utilisateurCourant);
        _service = new UtilisateurService(
            _contexte, new PasswordHasherService(), _utilisateurCourant, _audit, new HorlogeFactice());
    }

    [Fact]
    public async Task Un_utilisateur_peut_etre_cree()
    {
        var utilisateur = await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Employe
        });

        utilisateur.NomUtilisateur.Should().Be("karim");
        utilisateur.RoleNom.Should().Be("Employé");
        (await _contexte.Users.CountAsync()).Should().Be(2);
    }

    [Fact]
    public async Task Le_mot_de_passe_du_nouvel_utilisateur_est_hache()
    {
        await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Employe
        });

        var enregistre = await _contexte.Users.FirstAsync(u => u.UserName == "karim");
        enregistre.PasswordHash.Should().NotContain("Atelier@2026");
        new PasswordHasherService().Verifier("Atelier@2026", enregistre.PasswordHash).Should().BeTrue();
    }

    [Fact]
    public async Task Un_nom_d_utilisateur_deja_pris_est_refuse()
    {
        var action = async () => await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "ADMIN",
            NomComplet = "Doublon",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Employe
        });

        await action.Should().ThrowAsync<RegleMetierException>()
            .WithMessage("*déjà utilisé*");
    }

    [Fact]
    public async Task Un_employe_ne_peut_pas_creer_de_compte()
    {
        // Le menu cache l'écran ; le service refuse de son côté.
        _utilisateurCourant.Role = RoleUtilisateur.Employe;

        var action = async () => await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Administrateur
        });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*Seul un administrateur*");
        (await _contexte.Users.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task Le_dernier_administrateur_ne_peut_pas_etre_desactive()
    {
        var administrateur = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        _utilisateurCourant.UtilisateurId = 999;

        var action = async () => await _service.ChangerActivationAsync(administrateur.Id, false);

        await action.Should().ThrowAsync<RegleMetierException>()
            .WithMessage("*au moins un administrateur*");
    }

    [Fact]
    public async Task Un_administrateur_peut_etre_desactive_s_il_en_reste_un_autre()
    {
        var second = await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "amina",
            NomComplet = "Amina Saidi",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Administrateur
        });

        var premier = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        _utilisateurCourant.UtilisateurId = second.Id;

        await _service.ChangerActivationAsync(premier.Id, false);

        (await _contexte.Users.FirstAsync(u => u.Id == premier.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Un_utilisateur_ne_peut_pas_desactiver_son_propre_compte()
    {
        var administrateur = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        _utilisateurCourant.UtilisateurId = administrateur.Id;

        var action = async () => await _service.ChangerActivationAsync(administrateur.Id, false);

        await action.Should().ThrowAsync<RegleMetierException>()
            .WithMessage("*votre propre compte*");
    }

    [Fact]
    public async Task Un_compte_desactive_ne_peut_plus_se_connecter()
    {
        var utilisateur = await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Employe
        });

        await _service.ChangerActivationAsync(utilisateur.Id, false);

        var auth = new AuthService(_contexte, new PasswordHasherService(), new HorlogeFactice(), _audit,
            new UtilisateurCourantFactice(),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<AuthService>.Instance);

        var connexion = () => auth.ConnecterAsync(new CeramiPro.Application.DTOs.Auth.ConnexionRequete
        {
            NomUtilisateur = "karim", MotDePasse = "Atelier@2026"
        });

        await connexion.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task La_recherche_filtre_la_liste()
    {
        await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Employe
        });

        var page = await _service.ListerAsync(new PagedRequest { Recherche = "belhadj" });

        page.Total.Should().Be(1);
        page.Elements.Single().NomUtilisateur.Should().Be("karim");
    }

    [Fact]
    public async Task Un_utilisateur_introuvable_leve_une_erreur_explicite()
    {
        var action = async () => await _service.ObtenirAsync(4242);

        await action.Should().ThrowAsync<IntrouvableException>()
            .WithMessage("*introuvable*");
    }

    [Fact]
    public async Task La_creation_est_journalisee()
    {
        await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            Role = RoleUtilisateur.Employe
        });

        _audit.Traces.Should().ContainSingle(t => t.Action == CeramiPro.Domain.Enums.AuditAction.Creation);
    }

    public void Dispose() => _contexte.Dispose();
}
