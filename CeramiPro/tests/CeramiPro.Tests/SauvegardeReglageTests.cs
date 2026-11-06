using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Sauvegarde;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Infrastructure.Services;
using CeramiPro.Tests.Aides;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace CeramiPro.Tests;

/// <summary>La sauvegarde quotidienne se règle depuis le logiciel, par l'administrateur.</summary>
public class SauvegardeReglageTests : IDisposable
{
    private readonly UtilisateurCourantFactice _session = new();
    private readonly CeramiProDbContext _contexte;
    private readonly string _dossier = Path.Combine(Path.GetTempPath(), $"ceramipro-sauvegardes-{Guid.NewGuid():N}");

    public SauvegardeReglageTests() => _contexte = ContexteTest.Creer(_session);

    public void Dispose()
    {
        _contexte.Dispose();

        if (Directory.Exists(_dossier))
        {
            Directory.Delete(_dossier, recursive: true);
        }
    }

    private SauvegardeService Service()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Sauvegarde:Dossier"] = _dossier,
                ["Photos:Dossier"] = Path.Combine(_dossier, "photos")
            })
            .Build();

        return new SauvegardeService(_contexte, new StockagePhotos(configuration), new HorlogeFactice(),
            new AuditFactice(), _session, configuration, NullLogger<SauvegardeService>.Instance);
    }

    private string? Reglage(string cle) => _contexte.SystemSettings.Single(s => s.Key == cle).Value;

    [Fact]
    public async Task L_administrateur_active_la_sauvegarde_quotidienne()
    {
        await Service().ReglerAsync(new ReglageSauvegardeRequete { Active = true, Heure = "9:30", ConservationJours = 60 });

        Reglage(SauvegardeService.CleAutomatique).Should().Be("true");
        Reglage(SauvegardeService.CleHeure).Should().Be("09:30");
        Reglage(SauvegardeService.CleConservation).Should().Be("60");
    }

    [Fact]
    public async Task Le_reglage_se_modifie_sans_creer_de_doublon()
    {
        await Service().ReglerAsync(new ReglageSauvegardeRequete { Active = true, Heure = "13:00", ConservationJours = 30 });
        await Service().ReglerAsync(new ReglageSauvegardeRequete { Active = false, Heure = "14:15", ConservationJours = 10 });

        _contexte.SystemSettings.Count(s => s.Key.StartsWith("sauvegarde.")).Should().Be(3);
        Reglage(SauvegardeService.CleAutomatique).Should().Be("false");
        Reglage(SauvegardeService.CleHeure).Should().Be("14:15");
    }

    [Theory]
    [InlineData("25:00", 30)]
    [InlineData("midi", 30)]
    [InlineData("13:00", 0)]
    public async Task Une_heure_ou_une_duree_impossible_est_refusee(string heure, int jours)
    {
        var action = () => Service().ReglerAsync(new ReglageSauvegardeRequete
        {
            Active = true, Heure = heure, ConservationJours = jours
        });

        await action.Should().ThrowAsync<RegleMetierException>();
    }

    [Fact]
    public async Task Un_employe_ne_regle_pas_la_sauvegarde()
    {
        _session.Role = RoleUtilisateur.Employe;

        var action = () => Service().ReglerAsync(new ReglageSauvegardeRequete { Active = false });

        await action.Should().ThrowAsync<RegleMetierException>().WithMessage("*Seul un administrateur*");
    }
}
