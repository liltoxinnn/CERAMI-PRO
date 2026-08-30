using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

[Collection(CollectionApi.Nom)]
public class SanteEtBaseDeDonneesTests
{
    private readonly FabriqueApplication _fabrique;

    public SanteEtBaseDeDonneesTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task Le_serveur_repond_et_la_base_est_connectee()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.GetAsync("api/sante");
        var contenu = await reponse.Content.ReadFromJsonAsync<JsonElement>();

        reponse.StatusCode.Should().Be(HttpStatusCode.OK);
        contenu.GetProperty("application").GetString().Should().Be("CERAMIPRO");
        contenu.GetProperty("baseDeDonnees").GetString().Should().Be("Connectée");
    }

    [PostgresFact]
    public async Task Toutes_les_migrations_sont_appliquees()
    {
        var client = _fabrique.CreateClient();

        var contenu = await client.GetFromJsonAsync<JsonElement>("api/sante");

        contenu.GetProperty("migrationsEnAttente").GetInt32().Should().Be(0);
    }
}
