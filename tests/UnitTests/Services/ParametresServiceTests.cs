using CeramicWorkshop.Application.DTOs.Settings;
using CeramicWorkshop.Application.Services;
using CeramicWorkshop.Infrastructure.Data;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.Services;

public class ParametresServiceTests : IDisposable
{
    private readonly ApplicationDbContext _contexte;
    private readonly ParametresService _service;

    public ParametresServiceTests()
    {
        _contexte = ContexteTest.Creer();
        _service = new ParametresService(_contexte, new AuditFactice());
    }

    [Fact]
    public async Task La_configuration_par_defaut_correspond_a_l_Algerie()
    {
        var parametres = await _service.ObtenirAsync();

        parametres.CodeDevise.Should().Be("DZD");
        parametres.SymboleDevise.Should().Be("DA");
        parametres.FuseauHoraire.Should().Be("Africa/Algiers");
        parametres.FormatDate.Should().Be("dd/MM/yyyy");
        parametres.Culture.Should().Be("fr-DZ");
    }

    [Fact]
    public async Task Les_parametres_peuvent_etre_modifies()
    {
        var parametres = await _service.ObtenirAsync();
        parametres.NomAtelier = "Atelier Belhadj";
        parametres.Ville = "Alger";
        parametres.Telephone = "0550 12 34 56";

        var enregistres = await _service.ModifierAsync(parametres);

        enregistres.NomAtelier.Should().Be("Atelier Belhadj");
        enregistres.Ville.Should().Be("Alger");
        (await _service.ObtenirAsync()).Telephone.Should().Be("0550 12 34 56");
    }

    [Fact]
    public async Task La_devise_peut_etre_changee_pour_une_utilisation_hors_d_Algerie()
    {
        var parametres = await _service.ObtenirAsync();
        parametres.CodeDevise = "eur";
        parametres.SymboleDevise = "€";

        var enregistres = await _service.ModifierAsync(parametres);

        enregistres.CodeDevise.Should().Be("EUR");
        enregistres.SymboleDevise.Should().Be("€");
    }

    [Fact]
    public async Task Les_champs_laisses_vides_sont_enregistres_comme_non_renseignes()
    {
        var parametres = await _service.ObtenirAsync();
        parametres.Ville = "   ";

        var enregistres = await _service.ModifierAsync(parametres);

        enregistres.Ville.Should().BeNull();
    }

    public void Dispose() => _contexte.Dispose();
}
