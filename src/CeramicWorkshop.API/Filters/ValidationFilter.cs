using CeramicWorkshop.Application.Common;
using FluentValidation;
using Microsoft.AspNetCore.Mvc.Filters;

namespace CeramicWorkshop.API.Filters;

/// <summary>
/// Valide automatiquement les données reçues par les contrôleurs à l'aide des
/// validateurs FluentValidation. Les messages renvoyés sont en français.
/// </summary>
public class ValidationFilter : IAsyncActionFilter
{
    private readonly IServiceProvider _services;

    public ValidationFilter(IServiceProvider services) => _services = services;

    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var erreurs = new Dictionary<string, List<string>>();

        foreach (var argument in context.ActionArguments.Values.Where(EstFormulaire))
        {
            var typeValidateur = typeof(IValidator<>).MakeGenericType(argument!.GetType());

            if (_services.GetService(typeValidateur) is not IValidator validateur)
            {
                continue;
            }

            var contexteValidation = new ValidationContext<object>(argument);
            var resultat = await validateur.ValidateAsync(contexteValidation, context.HttpContext.RequestAborted);

            foreach (var echec in resultat.Errors)
            {
                var champ = string.IsNullOrWhiteSpace(echec.PropertyName) ? "Formulaire" : echec.PropertyName;

                if (!erreurs.TryGetValue(champ, out var messages))
                {
                    messages = new List<string>();
                    erreurs[champ] = messages;
                }

                messages.Add(echec.ErrorMessage);
            }
        }

        if (erreurs.Count > 0)
        {
            throw new ValidationFailedException(
                erreurs.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }

        await next();
    }

    /// <summary>
    /// Seuls les formulaires (objets complexes) sont validés ici. Les paramètres
    /// simples — texte d'une recherche, identifiant, date — n'ont pas de
    /// validateur de formulaire : leur appliquer celui d'un type partagé, comme
    /// le mot de passe qui est aussi un texte, rejetterait des requêtes valides.
    /// </summary>
    private static bool EstFormulaire(object? argument)
        => argument is not null
           && argument.GetType() is { IsClass: true, IsArray: false } type
           && type != typeof(string);
}
