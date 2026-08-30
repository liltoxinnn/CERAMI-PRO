namespace CeramicWorkshop.Application.Common;

/// <summary>
/// Réponse d'erreur renvoyée par l'API. Le message est rédigé en français
/// et peut être affiché tel quel à l'utilisateur.
/// </summary>
public class ErreurApi
{
    public string Message { get; set; } = "Une erreur est survenue.";

    /// <summary>Erreurs de saisie, regroupées par champ de formulaire.</summary>
    public Dictionary<string, string[]> Erreurs { get; set; } = new();

    /// <summary>Identifiant technique de l'incident, à communiquer au support.</summary>
    public string? Reference { get; set; }

    public IEnumerable<string> ToutesLesErreurs()
        => Erreurs.SelectMany(e => e.Value);
}
