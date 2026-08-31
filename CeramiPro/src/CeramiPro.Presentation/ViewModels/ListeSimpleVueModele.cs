using System.Reflection;
using CeramiPro.Application.Common;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Écran de liste pour les tables courtes — catégories, unités, fours,
/// recettes — que les services renvoient d'un bloc.
///
/// La recherche et la pagination se font ici, en mémoire : sur quelques
/// dizaines de lignes, cela évite d'imposer une pagination au service, et
/// l'écran se comporte exactement comme les autres.
/// </summary>
public abstract class ListeSimpleVueModele<TElement> : ListeVueModele<TElement>
{
    protected ListeSimpleVueModele(IServiceLangue langue, OutilsListe? outils = null)
        : base(langue, outils)
    {
    }

    /// <summary>Va chercher la totalité des lignes auprès du service métier.</summary>
    protected abstract Task<IReadOnlyList<TElement>> LireToutesAsync();

    protected override async Task<PagedResult<TElement>> LireAsync()
    {
        var toutes = await LireToutesAsync();
        var filtrees = Filtrer(toutes);

        var page = filtrees
            .Skip((Page - 1) * TaillePage)
            .Take(TaillePage)
            .ToList();

        return new PagedResult<TElement>(page, filtrees.Count, Page, TaillePage);
    }

    /// <summary>
    /// Ne garde que les lignes dont l'une des colonnes affichées contient le
    /// texte cherché : l'utilisateur retrouve ainsi ce qu'il voit à l'écran,
    /// sans avoir à deviner sur quel champ porte la recherche.
    /// </summary>
    private IReadOnlyList<TElement> Filtrer(IReadOnlyList<TElement> elements)
    {
        if (string.IsNullOrWhiteSpace(Recherche))
        {
            return elements;
        }

        var terme = Recherche.Trim();

        return elements
            .Where(element => Colonnes.Any(colonne =>
                Valeur(element, colonne.Propriete)?
                    .Contains(terme, StringComparison.CurrentCultureIgnoreCase) == true))
            .ToList();
    }

    private static string? Valeur(TElement element, string propriete)
        => element?.GetType()
            .GetProperty(propriete, BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(element)?
            .ToString();
}
