using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Application.DTOs.Identity;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

[Collection(CollectionApi.Nom)]
public class AutorisationsTests
{
    private readonly FabriqueApplication _fabrique;

    public AutorisationsTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task Sans_connexion_les_donnees_ne_sont_pas_accessibles()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.GetAsync("api/utilisateurs");

        reponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [PostgresFact]
    public async Task L_administrateur_accede_a_la_liste_des_utilisateurs()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var page = await client.GetFromJsonAsync<PagedResult<UtilisateurDto>>("api/utilisateurs");

        page!.Elements.Should().Contain(u => u.NomUtilisateur == "admin");
    }

    [PostgresFact]
    public async Task Un_caissier_ne_peut_pas_consulter_les_utilisateurs()
    {
        var administrateur = await _fabrique.CreerClientAdministrateurAsync();
        var identifiant = $"caissier{Guid.NewGuid():N}"[..20];

        var roles = await administrateur.GetFromJsonAsync<List<RoleDto>>("api/roles");
        var roleCaissier = roles!.First(r => r.Code == RoleCodes.Caissier);

        var creation = await administrateur.PostAsJsonAsync("api/utilisateurs", new CreerUtilisateurRequete
        {
            NomUtilisateur = identifiant,
            NomComplet = "Caissier de test",
            MotDePasse = "Caisse@2026",
            RoleId = roleCaissier.Id,
            DoitChangerMotDePasse = false
        });

        creation.StatusCode.Should().Be(HttpStatusCode.Created);

        var client = _fabrique.CreateClient();
        var connexion = await (await client.PostAsJsonAsync("api/authentification/connexion", new ConnexionRequete
        {
            NomUtilisateur = identifiant,
            MotDePasse = "Caisse@2026"
        })).Content.ReadFromJsonAsync<ConnexionReponse>();

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connexion!.JetonAcces);

        var reponse = await client.GetAsync("api/utilisateurs");

        reponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        connexion.Utilisateur.Droits.Should().Contain(PermissionCodes.VentesCreer);
    }
}
