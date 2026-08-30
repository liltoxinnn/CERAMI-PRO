using System.Net.Http.Json;
using CeramicWorkshop.Application.DTOs.Settings;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

[Collection(CollectionApi.Nom)]
public class ParametresApiTests
{
    private readonly FabriqueApplication _fabrique;

    public ParametresApiTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task Les_parametres_de_l_atelier_sont_configures_pour_l_Algerie()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var parametres = await client.GetFromJsonAsync<ParametresAtelierDto>("api/parametres");

        parametres!.CodeDevise.Should().Be("DZD");
        parametres.SymboleDevise.Should().Be("DA");
        parametres.FuseauHoraire.Should().Be("Africa/Algiers");
    }

    [PostgresFact]
    public async Task Les_parametres_sont_enregistres_et_relus()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();
        var parametres = await client.GetFromJsonAsync<ParametresAtelierDto>("api/parametres");

        parametres!.Ville = "Tlemcen";
        parametres.Telephone = "0770 00 00 00";

        var reponse = await client.PutAsJsonAsync("api/parametres", parametres);
        reponse.EnsureSuccessStatusCode();

        var relus = await client.GetFromJsonAsync<ParametresAtelierDto>("api/parametres");
        relus!.Ville.Should().Be("Tlemcen");
        relus.Telephone.Should().Be("0770 00 00 00");
    }

    [PostgresFact]
    public async Task Un_parametre_invalide_est_refuse_avec_un_message_en_francais()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();
        var parametres = await client.GetFromJsonAsync<ParametresAtelierDto>("api/parametres");

        parametres!.NomAtelier = string.Empty;

        var reponse = await client.PutAsJsonAsync("api/parametres", parametres);
        var erreur = await reponse.Content.ReadFromJsonAsync<CeramicWorkshop.Application.Common.ErreurApi>();

        erreur!.ToutesLesErreurs().Should().Contain("Le nom de l'atelier est obligatoire.");
    }
}
