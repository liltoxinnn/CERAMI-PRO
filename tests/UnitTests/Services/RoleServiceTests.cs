using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Application.Services;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Infrastructure.Data;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Services;

public class RoleServiceTests : IDisposable
{
    private readonly ApplicationDbContext _contexte;
    private readonly AuditFactice _audit = new();
    private readonly RoleService _service;

    public RoleServiceTests()
    {
        _contexte = ContexteTest.Creer();
        _service = new RoleService(_contexte, _audit);
    }

    [Fact]
    public async Task Les_quatre_roles_du_logiciel_sont_disponibles()
    {
        var roles = await _service.ListerAsync();

        roles.Select(r => r.Code).Should().BeEquivalentTo(
            RoleCodes.Administrateur, RoleCodes.Responsable, RoleCodes.Employe, RoleCodes.Caissier);
    }

    [Fact]
    public async Task L_administrateur_possede_tous_les_droits()
    {
        var roles = await _service.ListerAsync();
        var administrateur = roles.First(r => r.Code == RoleCodes.Administrateur);

        administrateur.Droits.Should().HaveCount(PermissionCodes.Catalogue.Count);
    }

    [Fact]
    public async Task Le_caissier_ne_peut_pas_gerer_la_production()
    {
        var roles = await _service.ListerAsync();
        var caissier = roles.First(r => r.Code == RoleCodes.Caissier);

        caissier.Droits.Should().Contain(PermissionCodes.VentesCreer);
        caissier.Droits.Should().NotContain(PermissionCodes.ProductionGerer);
    }

    [Fact]
    public async Task Les_droits_sont_presentes_par_module()
    {
        var modules = await _service.ListerDroitsParModuleAsync();

        modules.Should().NotBeEmpty();
        modules.Select(m => m.Module).Should().Contain("Production");
        modules.SelectMany(m => m.Droits).Should().HaveCount(PermissionCodes.Catalogue.Count);
    }

    [Fact]
    public async Task Les_droits_d_un_role_peuvent_etre_modifies()
    {
        var employe = await _contexte.Roles.FirstAsync(r => r.Code == RoleCodes.Employe);

        var resultat = await _service.ModifierDroitsAsync(employe.Id, new ModifierDroitsRoleRequete
        {
            CodesDroits = new List<string> { PermissionCodes.ProductionConsulter, PermissionCodes.QualiteConsulter }
        });

        resultat.Droits.Should().BeEquivalentTo(
            PermissionCodes.ProductionConsulter, PermissionCodes.QualiteConsulter);
    }

    [Fact]
    public async Task Le_role_administrateur_ne_peut_pas_etre_restreint()
    {
        var administrateur = await _contexte.Roles.FirstAsync(r => r.Code == RoleCodes.Administrateur);

        var action = async () => await _service.ModifierDroitsAsync(administrateur.Id, new ModifierDroitsRoleRequete
        {
            CodesDroits = new List<string> { PermissionCodes.VentesConsulter }
        });

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*tous les droits*");
    }

    [Fact]
    public async Task Un_droit_inconnu_est_refuse()
    {
        var employe = await _contexte.Roles.FirstAsync(r => r.Code == RoleCodes.Employe);

        var action = async () => await _service.ModifierDroitsAsync(employe.Id, new ModifierDroitsRoleRequete
        {
            CodesDroits = new List<string> { "droit.inexistant" }
        });

        await action.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task La_modification_des_droits_est_journalisee()
    {
        var employe = await _contexte.Roles.FirstAsync(r => r.Code == RoleCodes.Employe);

        await _service.ModifierDroitsAsync(employe.Id, new ModifierDroitsRoleRequete
        {
            CodesDroits = new List<string> { PermissionCodes.ProductionConsulter }
        });

        _audit.Traces.Should().ContainSingle(t => t.Entite == "Role");
    }

    public void Dispose() => _contexte.Dispose();
}
