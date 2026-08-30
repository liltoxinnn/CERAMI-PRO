using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Application.Services;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Infrastructure.Authentication;
using CeramicWorkshop.Infrastructure.Data;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Services;

public class UtilisateurServiceTests : IDisposable
{
    private readonly ApplicationDbContext _contexte;
    private readonly UtilisateurCourantFactice _utilisateurCourant = new();
    private readonly AuditFactice _audit = new();
    private readonly UtilisateurService _service;

    public UtilisateurServiceTests()
    {
        _contexte = ContexteTest.Creer(_utilisateurCourant);
        _service = new UtilisateurService(_contexte, new PasswordHasherService(), _utilisateurCourant, _audit);
    }

    private async Task<int> IdRoleAsync(string code)
        => (await _contexte.Roles.FirstAsync(r => r.Code == code)).Id;

    [Fact]
    public async Task Un_utilisateur_peut_etre_cree()
    {
        var utilisateur = await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            RoleId = await IdRoleAsync(RoleCodes.Employe)
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
            RoleId = await IdRoleAsync(RoleCodes.Employe)
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
            RoleId = await IdRoleAsync(RoleCodes.Employe)
        });

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*déjà utilisé*");
    }

    [Fact]
    public async Task Un_role_inexistant_est_refuse()
    {
        var action = async () => await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            RoleId = 9999
        });

        await action.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Le_dernier_administrateur_ne_peut_pas_etre_desactive()
    {
        var administrateur = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        _utilisateurCourant.UserId = 999;

        var action = async () => await _service.ChangerActivationAsync(administrateur.Id, false);

        await action.Should().ThrowAsync<BusinessRuleException>()
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
            RoleId = await IdRoleAsync(RoleCodes.Administrateur)
        });

        var premier = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        _utilisateurCourant.UserId = second.Id;

        await _service.ChangerActivationAsync(premier.Id, false);

        (await _contexte.Users.FirstAsync(u => u.Id == premier.Id)).IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task Un_utilisateur_ne_peut_pas_desactiver_son_propre_compte()
    {
        var administrateur = await _contexte.Users.FirstAsync(u => u.UserName == "admin");
        _utilisateurCourant.UserId = administrateur.Id;

        var action = async () => await _service.ChangerActivationAsync(administrateur.Id, false);

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*votre propre compte*");
    }

    [Fact]
    public async Task La_desactivation_invalide_la_session_en_cours()
    {
        var utilisateur = await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            RoleId = await IdRoleAsync(RoleCodes.Employe)
        });

        var enregistre = await _contexte.Users.FirstAsync(u => u.Id == utilisateur.Id);
        enregistre.RefreshToken = "jeton-en-cours";
        await _contexte.SaveChangesAsync();

        await _service.ChangerActivationAsync(utilisateur.Id, false);

        (await _contexte.Users.FirstAsync(u => u.Id == utilisateur.Id)).RefreshToken.Should().BeNull();
    }

    [Fact]
    public async Task La_recherche_filtre_la_liste()
    {
        await _service.CreerAsync(new CreerUtilisateurRequete
        {
            NomUtilisateur = "karim",
            NomComplet = "Karim Belhadj",
            MotDePasse = "Atelier@2026",
            RoleId = await IdRoleAsync(RoleCodes.Employe)
        });

        var page = await _service.ListerAsync(new PagedRequest { Recherche = "belhadj" });

        page.Total.Should().Be(1);
        page.Elements.Single().NomUtilisateur.Should().Be("karim");
    }

    [Fact]
    public async Task Un_utilisateur_introuvable_leve_une_erreur_explicite()
    {
        var action = async () => await _service.ObtenirAsync(4242);

        await action.Should().ThrowAsync<NotFoundException>()
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
            RoleId = await IdRoleAsync(RoleCodes.Employe)
        });

        _audit.Traces.Should().ContainSingle(t => t.Action == CeramicWorkshop.Domain.Enums.AuditAction.Creation);
    }

    public void Dispose() => _contexte.Dispose();
}
