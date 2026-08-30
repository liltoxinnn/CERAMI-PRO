using System.Net.Http.Headers;
using System.Net.Http.Json;
using CeramicWorkshop.Application.DTOs.Auth;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace CeramicWorkshop.IntegrationTests.Aides;

/// <summary>
/// Démarre l'API sur une base de données de test dédiée.
/// Les migrations et les données de départ sont appliquées automatiquement.
/// </summary>
public class FabriqueApplication : WebApplicationFactory<Program>
{
    /// <summary>Mot de passe du compte administrateur créé pour les tests.</summary>
    public const string MotDePasseAdministrateur = "Test@Ceramipro2026";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureHostConfiguration(configuration => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["ConnectionStrings:CeramicWorkshopDB"] = PostgresDisponible.ChaineConnexion,
                ["Jwt:Cle"] = "cle-de-test-suffisamment-longue-pour-la-signature-0123456789",
                ["Jwt:Emetteur"] = "CeramiPro",
                ["Jwt:Audience"] = "CeramiPro",
                ["Administrateur:MotDePasseInitial"] = MotDePasseAdministrateur,
                ["BaseDeDonnees:MigrerAuDemarrage"] = "true"
            }));

        return base.CreateHost(builder);
    }

    /// <summary>Crée un client déjà authentifié en tant qu'administrateur.</summary>
    public async Task<HttpClient> CreerClientAdministrateurAsync()
    {
        var client = CreateClient();

        var reponse = await client.PostAsJsonAsync("api/authentification/connexion", new ConnexionRequete
        {
            NomUtilisateur = "admin",
            MotDePasse = MotDePasseAdministrateur
        });

        reponse.EnsureSuccessStatusCode();

        var connexion = await reponse.Content.ReadFromJsonAsync<ConnexionReponse>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", connexion!.JetonAcces);

        return client;
    }
}

/// <summary>Partage la même instance de l'API entre tous les tests d'intégration.</summary>
[CollectionDefinition(Nom)]
public class CollectionApi : ICollectionFixture<FabriqueApplication>
{
    public const string Nom = "API CeramiPro";
}
