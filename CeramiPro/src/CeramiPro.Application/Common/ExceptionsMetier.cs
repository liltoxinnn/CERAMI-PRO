namespace CeramiPro.Application.Common;

/// <summary>
/// Règle de gestion non respectée. Le message est rédigé en français, à
/// destination de l'utilisateur : il est affiché tel quel à l'écran.
/// </summary>
public class RegleMetierException : Exception
{
    public RegleMetierException(string message, IReadOnlyList<string>? details = null)
        : base(message)
        => Details = details ?? Array.Empty<string>();

    /// <summary>Précisions listées sous le message, par exemple les matières manquantes.</summary>
    public IReadOnlyList<string> Details { get; }
}

/// <summary>Fiche demandée introuvable.</summary>
public class IntrouvableException : Exception
{
    public IntrouvableException(string message) : base(message) { }

    public static IntrouvableException Pour(string entite, int id)
        => new($"{entite} n° {id} est introuvable.");
}
