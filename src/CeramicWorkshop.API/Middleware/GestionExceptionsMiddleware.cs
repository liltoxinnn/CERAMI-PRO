using System.Text.Json;
using CeramicWorkshop.Application.Common;

namespace CeramicWorkshop.API.Middleware;

/// <summary>
/// Transforme toute exception en réponse claire et en français.
/// Les détails techniques restent dans les journaux du serveur : ils ne sont
/// jamais renvoyés au navigateur.
/// </summary>
public class GestionExceptionsMiddleware
{
    private readonly RequestDelegate _suivant;
    private readonly ILogger<GestionExceptionsMiddleware> _journal;

    public GestionExceptionsMiddleware(RequestDelegate suivant, ILogger<GestionExceptionsMiddleware> journal)
    {
        _suivant = suivant;
        _journal = journal;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _suivant(context);
        }
        catch (Exception exception)
        {
            await EcrireReponseAsync(context, exception);
        }
    }

    private async Task EcrireReponseAsync(HttpContext context, Exception exception)
    {
        var reference = context.TraceIdentifier;

        var (code, erreur) = exception switch
        {
            ValidationFailedException validation => (
                StatusCodes.Status400BadRequest,
                new ErreurApi
                {
                    Message = validation.Message,
                    Erreurs = validation.Erreurs.ToDictionary(e => e.Key, e => e.Value)
                }),

            BusinessRuleException metier => (
                StatusCodes.Status400BadRequest,
                new ErreurApi
                {
                    Message = metier.Message,
                    Erreurs = metier.Details.Count > 0
                        ? new Dictionary<string, string[]> { ["Détails"] = metier.Details.ToArray() }
                        : new Dictionary<string, string[]>()
                }),

            NotFoundException introuvable => (
                StatusCodes.Status404NotFound,
                new ErreurApi { Message = introuvable.Message }),

            ForbiddenException interdit => (
                StatusCodes.Status403Forbidden,
                new ErreurApi { Message = interdit.Message }),

            OperationCanceledException => (
                CodesEtatSupplementaires.RequeteInterrompueParLeClient,
                new ErreurApi { Message = "Opération interrompue." }),

            _ => (
                StatusCodes.Status500InternalServerError,
                new ErreurApi
                {
                    Message = "Une erreur inattendue est survenue. L'opération n'a pas été enregistrée.",
                    Reference = reference
                })
        };

        if (code == StatusCodes.Status500InternalServerError)
        {
            _journal.LogError(exception, "Erreur non gérée (référence {Reference}) sur {Chemin}.",
                reference, context.Request.Path);
        }
        else
        {
            _journal.LogInformation("Requête refusée ({Code}) sur {Chemin} : {Message}",
                code, context.Request.Path, erreur.Message);
        }

        if (context.Response.HasStarted)
        {
            return;
        }

        context.Response.Clear();
        context.Response.StatusCode = code;
        context.Response.ContentType = "application/json; charset=utf-8";

        await context.Response.WriteAsync(JsonSerializer.Serialize(erreur, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        }));
    }
}

/// <summary>Code d'état non standard utilisé lorsque le client interrompt la requête.</summary>
internal static class CodesEtatSupplementaires
{
    public const int RequeteInterrompueParLeClient = 499;
}
