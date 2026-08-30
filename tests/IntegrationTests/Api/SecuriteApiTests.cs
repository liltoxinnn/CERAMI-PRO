using System.Net;
using System.Net.Http.Headers;
using System.Text;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

/// <summary>
/// Vérifications de sécurité demandées par le cahier des charges : en-têtes de
/// réponse, contrôle réel des fichiers déposés et confidentialité des accès.
/// </summary>
[Collection(CollectionApi.Nom)]
public class SecuriteApiTests
{
    private readonly FabriqueApplication _fabrique;

    public SecuriteApiTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task Les_reponses_portent_les_entetes_de_securite()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.GetAsync("api/sante");

        reponse.Headers.GetValues("X-Content-Type-Options").Should().Contain("nosniff");
        reponse.Headers.GetValues("X-Frame-Options").Should().Contain("DENY");
        reponse.Headers.GetValues("Referrer-Policy").Should().Contain("no-referrer");
    }

    [PostgresFact]
    public async Task Un_fichier_deguise_en_image_est_refuse()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        // Un script renommé en « .png » et annoncé comme une image.
        var contenu = new ByteArrayContent(Encoding.UTF8.GetBytes("<script>alert(1)</script>"));
        contenu.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        using var formulaire = new MultipartFormDataContent { { contenu, "fichier", "piege.png" } };

        var reponse = await client.PostAsync("api/fichiers", formulaire);

        reponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        (await reponse.Content.ReadAsStringAsync()).Should().Contain("valide");
    }

    [PostgresFact]
    public async Task Une_vraie_image_png_est_acceptee()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        // Image PNG minimale valide (1 pixel).
        var png = Convert.FromBase64String(
            "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==");

        var contenu = new ByteArrayContent(png);
        contenu.Headers.ContentType = new MediaTypeHeaderValue("image/png");

        using var formulaire = new MultipartFormDataContent { { contenu, "fichier", "point.png" } };

        var reponse = await client.PostAsync("api/fichiers", formulaire);

        reponse.EnsureSuccessStatusCode();
        (await reponse.Content.ReadAsStringAsync()).Should().Contain("/fichiers/");
    }

    [PostgresFact]
    public async Task Un_mot_de_passe_n_est_jamais_renvoye_par_l_API()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var reponse = await client.GetAsync("api/utilisateurs");
        reponse.EnsureSuccessStatusCode();

        var texte = await reponse.Content.ReadAsStringAsync();

        texte.Should().NotContain("PasswordHash");
        texte.Should().NotContain("motDePasse");
        texte.Should().NotContain(FabriqueApplication.MotDePasseAdministrateur);
    }

    [PostgresFact]
    public async Task Une_recherche_globale_exige_une_session_ouverte()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.GetAsync("api/recherche?terme=vase");

        reponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
