namespace CeramicWorkshop.Application.Common;

/// <summary>
/// Résultat d'une opération métier. Les messages sont rédigés en français
/// car ils sont affichés tels quels à l'utilisateur.
/// </summary>
public class Result
{
    protected Result(bool succes, string? message, IReadOnlyList<string>? erreurs)
    {
        Succes = succes;
        Message = message;
        Erreurs = erreurs ?? Array.Empty<string>();
    }

    public bool Succes { get; }
    public string? Message { get; }
    public IReadOnlyList<string> Erreurs { get; }

    public static Result Reussi(string? message = null) => new(true, message, null);

    public static Result Echec(string message, params string[] erreurs) => new(false, message, erreurs);
}

public class Result<T> : Result
{
    private Result(bool succes, T? valeur, string? message, IReadOnlyList<string>? erreurs)
        : base(succes, message, erreurs) => Valeur = valeur;

    public T? Valeur { get; }

    public static Result<T> Reussi(T valeur, string? message = null) => new(true, valeur, message, null);

    public static new Result<T> Echec(string message, params string[] erreurs) => new(false, default, message, erreurs);
}
