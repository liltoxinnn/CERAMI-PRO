using System.Text.Json;
using CeramicWorkshop.Web.Models;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CeramicWorkshop.Web.Services;

/// <summary>
/// Conserve la session dans le navigateur, sous forme chiffrée par le serveur,
/// afin que l'utilisateur reste connecté après un rafraîchissement de la page.
/// </summary>
public class StockageSession
{
    private const string Cle = "ceramipro.session";

    private readonly ProtectedLocalStorage _stockage;
    private readonly ILogger<StockageSession> _journal;

    public StockageSession(ProtectedLocalStorage stockage, ILogger<StockageSession> journal)
    {
        _stockage = stockage;
        _journal = journal;
    }

    public async Task<SessionEnregistree?> LireAsync()
    {
        try
        {
            var resultat = await _stockage.GetAsync<string>(Cle);

            if (!resultat.Success || string.IsNullOrWhiteSpace(resultat.Value))
            {
                return null;
            }

            return JsonSerializer.Deserialize<SessionEnregistree>(resultat.Value);
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            _journal.LogWarning(ex, "Session enregistrée illisible : elle est ignorée.");
            return null;
        }
    }

    public async Task EcrireAsync(SessionEnregistree session)
        => await _stockage.SetAsync(Cle, JsonSerializer.Serialize(session));

    public async Task EffacerAsync() => await _stockage.DeleteAsync(Cle);
}
