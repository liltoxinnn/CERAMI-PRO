namespace CeramicWorkshop.API.Middleware;

/// <summary>
/// Ajoute les en-têtes de sécurité recommandés à chaque réponse.
///
/// « nosniff » est le plus important ici : les photos et justificatifs déposés
/// par l'atelier sont servis depuis le serveur, et cet en-tête empêche un
/// navigateur de les interpréter comme autre chose que leur type déclaré.
/// </summary>
public class EntetesSecuriteMiddleware
{
    private readonly RequestDelegate _suivant;

    public EntetesSecuriteMiddleware(RequestDelegate suivant) => _suivant = suivant;

    public async Task InvokeAsync(HttpContext contexte)
    {
        var entetes = contexte.Response.Headers;

        entetes["X-Content-Type-Options"] = "nosniff";
        entetes["X-Frame-Options"] = "DENY";
        entetes["Referrer-Policy"] = "no-referrer";
        entetes["Cross-Origin-Resource-Policy"] = "same-site";

        await _suivant(contexte);
    }
}
