using System.Net;
using System.Net.Http.Json;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

[Collection(CollectionApi.Nom)]
public class AuthentificationTests
{
    private readonly FabriqueApplication _fabrique;

    public AuthentificationTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task L_administrateur_cree_a_l_installation_peut_se_connecter()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.PostAsJsonAsync("api/authentification/connexion", new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = FabriqueApplication.MotDePasseAdministrateur
        });

        var connexion = await reponse.Content.ReadFromJsonAsync<ConnexionReponse>();

        reponse.StatusCode.Should().Be(HttpStatusCode.OK);
        connexion!.JetonAcces.Should().NotBeNullOrWhiteSpace();
        connexion.Utilisateur.RoleCode.Should().Be(RoleCodes.Administrateur);
        connexion.Utilisateur.Droits.Should().HaveCount(PermissionCodes.Catalogue.Count);
    }

    [PostgresFact]
    public async Task Un_mot_de_passe_incorrect_renvoie_un_message_en_francais()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.PostAsJsonAsync("api/authentification/connexion", new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = "mauvais-mot-de-passe"
        });

        var erreur = await reponse.Content.ReadFromJsonAsync<ErreurApi>();

        reponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        erreur!.Message.Should().Be("Nom d'utilisateur ou mot de passe incorrect.");
    }

    [PostgresFact]
    public async Task Un_formulaire_incomplet_est_refuse_avec_le_detail_des_champs()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.PostAsJsonAsync("api/authentification/connexion", new ConnexionRequete
        {
            NomUtilisateur = string.Empty,
            MotDePasse = string.Empty
        });

        var erreur = await reponse.Content.ReadFromJsonAsync<ErreurApi>();

        reponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        erreur!.ToutesLesErreurs().Should().Contain("Le nom d'utilisateur est obligatoire.");
        erreur.ToutesLesErreurs().Should().Contain("Le mot de passe est obligatoire.");
    }

    [PostgresFact]
    public async Task Le_profil_est_accessible_avec_un_jeton_valide()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var profil = await client.GetFromJsonAsync<UtilisateurConnecteDto>("api/authentification/profil");

        profil!.NomUtilisateur.Should().Be("admin");
        profil.Droits.Should().Contain(PermissionCodes.UtilisateursGerer);
    }

    [PostgresFact]
    public async Task Le_jeton_de_renouvellement_prolonge_la_session()
    {
        var client = _fabrique.CreateClient();

        var connexion = await (await client.PostAsJsonAsync("api/authentification/connexion", new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = FabriqueApplication.MotDePasseAdministrateur
        })).Content.ReadFromJsonAsync<ConnexionReponse>();

        var reponse = await client.PostAsJsonAsync("api/authentification/renouvellement",
            new RenouvellementRequete { JetonRenouvellement = connexion!.JetonRenouvellement });

        reponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }
}
