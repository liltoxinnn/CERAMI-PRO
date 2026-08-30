using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Web.Models;

namespace CeramicWorkshop.Web.Services;

/// <summary>
/// Appelle l'API du logiciel en joignant le jeton de l'utilisateur.
/// Si le jeton a expiré, une tentative de renouvellement est effectuée
/// avant de demander une nouvelle connexion.
/// </summary>
public class ClientApi
{
    private static readonly JsonSerializerOptions OptionsJson = new(JsonSerializerDefaults.Web);

    private readonly HttpClient _http;
    private readonly SessionUtilisateur _session;
    private readonly ILogger<ClientApi> _journal;

    /// <summary>Déclenché lorsque la session n'est plus valable et que l'utilisateur doit se reconnecter.</summary>
    public event Func<Task>? SessionPerdue;

    public ClientApi(HttpClient http, SessionUtilisateur session, ILogger<ClientApi> journal)
    {
        _http = http;
        _session = session;
        _journal = journal;
    }

    public Task<ResultatApi<T>> ObtenirAsync<T>(string chemin, CancellationToken cancellationToken = default)
        => EnvoyerAsync<T>(() => new HttpRequestMessage(HttpMethod.Get, chemin), cancellationToken);

    public Task<ResultatApi<TReponse>> PosterAsync<TReponse>(
        string chemin, object? corps = null, CancellationToken cancellationToken = default)
        => EnvoyerAsync<TReponse>(() => CreerRequete(HttpMethod.Post, chemin, corps), cancellationToken);

    public Task<ResultatApi<TReponse>> MettreAJourAsync<TReponse>(
        string chemin, object corps, CancellationToken cancellationToken = default)
        => EnvoyerAsync<TReponse>(() => CreerRequete(HttpMethod.Put, chemin, corps), cancellationToken);

    public Task<ResultatApi<object>> SupprimerAsync(string chemin, CancellationToken cancellationToken = default)
        => EnvoyerAsync<object>(() => new HttpRequestMessage(HttpMethod.Delete, chemin), cancellationToken);

    private static HttpRequestMessage CreerRequete(HttpMethod methode, string chemin, object? corps)
    {
        var requete = new HttpRequestMessage(methode, chemin);

        if (corps is not null)
        {
            requete.Content = JsonContent.Create(corps, options: OptionsJson);
        }

        return requete;
    }

    private async Task<ResultatApi<T>> EnvoyerAsync<T>(
        Func<HttpRequestMessage> creerRequete, CancellationToken cancellationToken)
    {
        // Un refus sur un appel anonyme (connexion) doit afficher le message du serveur ;
        // un refus alors qu'une session existe signifie que le jeton n'est plus valable.
        var sessionActive = !string.IsNullOrWhiteSpace(_session.JetonAcces);

        try
        {
            var reponse = await ExecuterAsync(creerRequete(), cancellationToken);

            if (reponse.StatusCode == HttpStatusCode.Unauthorized
                && sessionActive
                && await RenouvelerAsync(cancellationToken))
            {
                reponse.Dispose();
                reponse = await ExecuterAsync(creerRequete(), cancellationToken);
            }

            using (reponse)
            {
                if (reponse.IsSuccessStatusCode)
                {
                    if (reponse.StatusCode == HttpStatusCode.NoContent || typeof(T) == typeof(object))
                    {
                        return ResultatApi<T>.Reussi(default);
                    }

                    var valeur = await reponse.Content.ReadFromJsonAsync<T>(OptionsJson, cancellationToken);
                    return ResultatApi<T>.Reussi(valeur);
                }

                if (reponse.StatusCode == HttpStatusCode.Unauthorized && sessionActive)
                {
                    await SignalerSessionPerdueAsync();
                    return ResultatApi<T>.Echec("Votre session a expiré. Veuillez vous reconnecter.");
                }

                if (reponse.StatusCode == HttpStatusCode.Forbidden)
                {
                    return ResultatApi<T>.Echec("Vous n'avez pas l'autorisation d'effectuer cette action.");
                }

                return ResultatApi<T>.Echec(await LireErreurAsync(reponse, cancellationToken));
            }
        }
        catch (HttpRequestException ex)
        {
            _journal.LogError(ex, "Le serveur de l'application est injoignable.");
            return ResultatApi<T>.Echec(
                "Le serveur de l'application est injoignable. Vérifiez qu'il est démarré, puis réessayez.");
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            _journal.LogError(ex, "Délai d'attente dépassé lors de l'appel au serveur.");
            return ResultatApi<T>.Echec("Le serveur met trop de temps à répondre. Réessayez dans un instant.");
        }
    }

    private async Task<HttpResponseMessage> ExecuterAsync(HttpRequestMessage requete, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(_session.JetonAcces))
        {
            requete.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.JetonAcces);
        }

        return await _http.SendAsync(requete, cancellationToken);
    }

    private async Task<bool> RenouvelerAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_session.JetonRenouvellement))
        {
            return false;
        }

        var requete = CreerRequete(HttpMethod.Post, "api/authentification/renouvellement",
            new RenouvellementRequete { JetonRenouvellement = _session.JetonRenouvellement });

        using var reponse = await _http.SendAsync(requete, cancellationToken);

        if (!reponse.IsSuccessStatusCode)
        {
            return false;
        }

        var contenu = await reponse.Content.ReadFromJsonAsync<ConnexionReponse>(OptionsJson, cancellationToken);

        if (contenu is null)
        {
            return false;
        }

        _session.Definir(contenu);
        return true;
    }

    private async Task SignalerSessionPerdueAsync()
    {
        _session.Effacer();

        if (SessionPerdue is not null)
        {
            await SessionPerdue.Invoke();
        }
    }

    private async Task<ErreurApi> LireErreurAsync(HttpResponseMessage reponse, CancellationToken cancellationToken)
    {
        try
        {
            var erreur = await reponse.Content.ReadFromJsonAsync<ErreurApi>(OptionsJson, cancellationToken);

            if (erreur is not null && !string.IsNullOrWhiteSpace(erreur.Message))
            {
                return erreur;
            }
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            _journal.LogWarning(ex, "Réponse d'erreur illisible ({Code}).", (int)reponse.StatusCode);
        }

        return new ErreurApi
        {
            Message = $"L'opération a échoué (code {(int)reponse.StatusCode}). Réessayez ou contactez l'administrateur."
        };
    }
}
