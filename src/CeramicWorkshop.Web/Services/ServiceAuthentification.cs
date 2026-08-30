using CeramicWorkshop.Application.DTOs.Auth;
using CeramicWorkshop.Web.Models;

namespace CeramicWorkshop.Web.Services;

/// <summary>Connexion, restauration de session et déconnexion côté interface.</summary>
public class ServiceAuthentification
{
    private readonly ClientApi _api;
    private readonly SessionUtilisateur _session;
    private readonly StockageSession _stockage;

    public ServiceAuthentification(ClientApi api, SessionUtilisateur session, StockageSession stockage)
    {
        _api = api;
        _session = session;
        _stockage = stockage;

        // L'API prévient lorsqu'un jeton n'est plus valable : la session locale est alors effacée.
        _api.SessionPerdue += DeconnexionLocaleAsync;
    }

    public SessionUtilisateur Session => _session;

    /// <summary>Déclenché après une connexion, une déconnexion ou une restauration de session.</summary>
    public event Action? EtatModifie;

    public async Task<ResultatApi<ConnexionReponse>> ConnexionAsync(
        ConnexionRequete requete, CancellationToken cancellationToken = default)
    {
        var resultat = await _api.PosterAsync<ConnexionReponse>(
            "api/authentification/connexion", requete, cancellationToken);

        if (!resultat.Succes || resultat.Valeur is null)
        {
            return resultat;
        }

        _session.Definir(resultat.Valeur);

        await _stockage.EcrireAsync(new SessionEnregistree(
            resultat.Valeur.JetonAcces,
            resultat.Valeur.JetonRenouvellement,
            resultat.Valeur.ExpirationJeton,
            resultat.Valeur.Utilisateur));

        EtatModifie?.Invoke();
        return resultat;
    }

    /// <summary>
    /// Restaure la session enregistrée dans le navigateur.
    /// Si le jeton a expiré, un renouvellement est tenté automatiquement.
    /// </summary>
    public async Task<bool> RestaurerAsync(CancellationToken cancellationToken = default)
    {
        if (_session.EstConnecte)
        {
            return true;
        }

        var enregistree = await _stockage.LireAsync();

        if (enregistree is null)
        {
            return false;
        }

        _session.Definir(new ConnexionReponse(
            enregistree.JetonAcces,
            enregistree.Expiration,
            enregistree.JetonRenouvellement,
            enregistree.Profil));

        // Le profil est revalidé auprès du serveur : droits révoqués, compte désactivé…
        var profil = await _api.ObtenirAsync<UtilisateurConnecteDto>("api/authentification/profil", cancellationToken);

        if (!profil.Succes || profil.Valeur is null)
        {
            await DeconnexionLocaleAsync();
            return false;
        }

        _session.Definir(new ConnexionReponse(
            _session.JetonAcces!,
            _session.Expiration ?? DateTime.UtcNow,
            _session.JetonRenouvellement!,
            profil.Valeur));

        await _stockage.EcrireAsync(new SessionEnregistree(
            _session.JetonAcces!,
            _session.JetonRenouvellement!,
            _session.Expiration ?? DateTime.UtcNow,
            profil.Valeur));

        EtatModifie?.Invoke();
        return true;
    }

    public async Task DeconnexionAsync(CancellationToken cancellationToken = default)
    {
        if (_session.EstConnecte)
        {
            await _api.PosterAsync<object>("api/authentification/deconnexion", null, cancellationToken);
        }

        await DeconnexionLocaleAsync();
    }

    /// <summary>Efface la session sans appeler le serveur (jeton déjà invalide).</summary>
    public async Task DeconnexionLocaleAsync()
    {
        _session.Effacer();
        await _stockage.EffacerAsync();
        EtatModifie?.Invoke();
    }
}
