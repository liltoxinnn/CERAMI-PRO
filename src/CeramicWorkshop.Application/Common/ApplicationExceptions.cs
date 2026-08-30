namespace CeramicWorkshop.Application.Common;

/// <summary>Violation d'une règle métier. Le message est affiché à l'utilisateur en français.</summary>
public class BusinessRuleException : Exception
{
    public BusinessRuleException(string message) : base(message)
    {
    }

    public BusinessRuleException(string message, IReadOnlyList<string> details) : base(message)
        => Details = details;

    public IReadOnlyList<string> Details { get; } = Array.Empty<string>();
}

/// <summary>Élément introuvable.</summary>
public class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException Pour(string entite, object identifiant)
        => new($"{entite} introuvable (identifiant : {identifiant}).");
}

/// <summary>Données de formulaire invalides.</summary>
public class ValidationFailedException : Exception
{
    public ValidationFailedException(IReadOnlyDictionary<string, string[]> erreurs)
        : base("Les informations saisies sont incomplètes ou incorrectes.")
        => Erreurs = erreurs;

    public IReadOnlyDictionary<string, string[]> Erreurs { get; }
}

/// <summary>Action refusée : l'utilisateur ne possède pas le droit nécessaire.</summary>
public class ForbiddenException : Exception
{
    public ForbiddenException(string message = "Vous n'avez pas l'autorisation d'effectuer cette action.")
        : base(message)
    {
    }
}
