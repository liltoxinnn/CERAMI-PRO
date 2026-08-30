using System.Net;
using System.Net.Http.Json;
using CeramicWorkshop.Application.DTOs.Codes;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

[Collection(CollectionApi.Nom)]
public class CodesApiTests
{
    private readonly FabriqueApplication _fabrique;

    public CodesApiTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task Un_code_inconnu_repond_sans_erreur_de_validation()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        // Ce code ne contient aucun chiffre : il ne doit pas être confondu avec
        // un mot de passe par le contrôle automatique des formulaires.
        var reponse = await client.GetAsync("api/codes/scan?code=CODE-QUI-N-EXISTE-PAS");

        reponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var resultat = await reponse.Content.ReadFromJsonAsync<ResultatScanDto>();

        resultat!.Trouve.Should().BeFalse();
        resultat.Cible.Should().Be(CibleScan.Inconnu);
        resultat.Libelle.Should().NotBeNullOrWhiteSpace();
    }

    [PostgresFact]
    public async Task Le_scan_exige_une_session_ouverte()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.GetAsync("api/codes/scan?code=PRD-2026-0001");

        reponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [PostgresFact]
    public async Task Une_planche_sans_produit_est_refusee_avec_un_message_en_francais()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var reponse = await client.PostAsJsonAsync("api/codes/etiquettes", new EtiquettesRequete());

        reponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reponse.Content.ReadAsStringAsync()).Should().Contain("produit");
    }
}
