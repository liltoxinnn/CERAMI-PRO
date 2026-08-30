using CeramicWorkshop.Application.Common;

namespace CeramicWorkshop.Web.Models;

/// <summary>Résultat d'un appel à l'API, prêt à être affiché dans un formulaire.</summary>
public class ResultatApi<T>
{
    private ResultatApi(bool succes, T? valeur, ErreurApi? erreur)
    {
        Succes = succes;
        Valeur = valeur;
        Erreur = erreur;
    }

    public bool Succes { get; }
    public T? Valeur { get; }
    public ErreurApi? Erreur { get; }

    public string Message => Erreur?.Message ?? string.Empty;

    /// <summary>Messages détaillés à afficher sous le formulaire.</summary>
    public IReadOnlyList<string> Details => Erreur?.ToutesLesErreurs().ToList() ?? new List<string>();

    public static ResultatApi<T> Reussi(T? valeur) => new(true, valeur, null);

    public static ResultatApi<T> Echec(ErreurApi erreur) => new(false, default, erreur);

    public static ResultatApi<T> Echec(string message) => new(false, default, new ErreurApi { Message = message });
}

/// <summary>Fichier enregistré sur le serveur.</summary>
public record FichierDepose(string Chemin, string NomOrigine, long Taille);

/// <summary>Adresse du serveur applicatif, utilisée pour afficher les photos déposées.</summary>
public record AdresseServeur(string Base)
{
    /// <summary>Construit l'adresse complète d'un fichier enregistré sur le serveur.</summary>
    public string Fichier(string? chemin)
        => string.IsNullOrWhiteSpace(chemin) ? string.Empty : $"{Base}{chemin}";
}
