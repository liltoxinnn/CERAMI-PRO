using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using CeramicWorkshop.Application.DTOs.Sauvegarde;
using CeramicWorkshop.IntegrationTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.IntegrationTests.Api;

[Collection(CollectionApi.Nom)]
public class SauvegardeApiTests
{
    private readonly FabriqueApplication _fabrique;

    public SauvegardeApiTests(FabriqueApplication fabrique) => _fabrique = fabrique;

    [PostgresFact]
    public async Task Une_sauvegarde_contient_les_donnees_de_toutes_les_tables()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var creation = await client.PostAsync("api/sauvegardes", null);
        creation.EnsureSuccessStatusCode();

        var sauvegarde = await creation.Content.ReadFromJsonAsync<SauvegardeDto>();
        sauvegarde!.NomFichier.Should().EndWith(".zip");
        sauvegarde.TailleOctets.Should().BeGreaterThan(0);

        var telechargement = await client.GetAsync($"api/sauvegardes/{sauvegarde.NomFichier}");
        telechargement.EnsureSuccessStatusCode();

        var contenu = await telechargement.Content.ReadAsByteArrayAsync();

        using var archive = new ZipArchive(new MemoryStream(contenu), ZipArchiveMode.Read);

        archive.Entries.Should().Contain(e => e.FullName == "LISEZ-MOI.txt");
        archive.Entries.Count(e => e.FullName.StartsWith("donnees/")).Should().BeGreaterThan(40);

        // La table des droits est toujours remplie par l'amorçage : elle permet
        // de vérifier que l'export contient bien les lignes, pas seulement les
        // en-têtes de colonnes.
        var droits = archive.Entries.First(e => e.FullName == "donnees/Permissions.csv");

        using var lecture = new StreamReader(droits.Open(), Encoding.UTF8);
        var texte = await lecture.ReadToEndAsync();

        texte.Should().Contain("Code");
        texte.Should().Contain("produits.consulter");

        // Nettoyage : l'archive de test ne reste pas sur le disque.
        (await client.DeleteAsync($"api/sauvegardes/{sauvegarde.NomFichier}"))
            .EnsureSuccessStatusCode();
    }

    [PostgresFact]
    public async Task L_etat_des_sauvegardes_ne_devoile_aucun_identifiant_de_connexion()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var reponse = await client.GetAsync("api/sauvegardes");
        reponse.EnsureSuccessStatusCode();

        var texte = await reponse.Content.ReadAsStringAsync();

        texte.Should().NotContain("Password");
        texte.Should().NotContain("Username");
        texte.Should().NotContain("Host=");
    }

    [PostgresFact]
    public async Task Un_nom_de_fichier_qui_sort_du_dossier_est_refuse()
    {
        var client = await _fabrique.CreerClientAdministrateurAsync();

        var reponse = await client.GetAsync("api/sauvegardes/..%2F..%2Fappsettings.json");

        reponse.StatusCode.Should().BeOneOf(
            HttpStatusCode.BadRequest, HttpStatusCode.NotFound);
    }

    [PostgresFact]
    public async Task Les_sauvegardes_sont_reservees_aux_administrateurs()
    {
        var client = _fabrique.CreateClient();

        var reponse = await client.GetAsync("api/sauvegardes");

        reponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
