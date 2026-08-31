using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Base commune à tous les écrans qui présentent une liste : recherche,
/// pagination, rechargement et sélection.
///
/// Écrire ce comportement une seule fois évite dix-huit variantes
/// légèrement différentes, et rend la même ergonomie partout.
/// </summary>
public abstract partial class ListeVueModele<TElement> : VueModeleBase
{
    protected readonly IServiceLangue Langue;

    protected ListeVueModele(IServiceLangue langue)
    {
        Langue = langue;
        Langue.LangueChangee += RafraichirTextes;
    }

    /// <summary>Éléments affichés dans le tableau.</summary>
    public ObservableCollection<TElement> Elements { get; } = new();

    [ObservableProperty]
    private TElement? _elementSelectionne;

    [ObservableProperty]
    private string _recherche = string.Empty;

    [ObservableProperty]
    private int _page = 1;

    [ObservableProperty]
    private int _nombreTotal;

    /// <summary>Nombre de lignes par page.</summary>
    public int TaillePage { get; set; } = 25;

    public int NombrePages => NombreTotal == 0 ? 1 : (int)Math.Ceiling((double)NombreTotal / TaillePage);

    public bool PagePrecedenteDisponible => Page > 1;

    public bool PageSuivanteDisponible => Page < NombrePages;

    /// <summary>Vrai quand la recherche n'a rien donné, pour afficher un message.</summary>
    public bool AucunResultat => !ChargementEnCours && Elements.Count == 0;

    public string LibelleRechercher => Langue["action.rechercher"];
    public string LibelleActualiser => Langue["action.actualiser"];
    public string LibelleAjouter => Langue["action.ajouter"];
    public string LibelleModifier => Langue["action.modifier"];
    public string LibelleSupprimer => Langue["action.supprimer"];
    public string LibelleAucunResultat => Langue["etat.aucunResultat"];

    /// <summary>Colonnes du tableau, déclarées par chaque écran.</summary>
    public abstract IReadOnlyList<ColonneListe> Colonnes { get; }

    /// <summary>Va chercher une page de résultats auprès du service métier.</summary>
    protected abstract Task<PagedResult<TElement>> LireAsync();

    public override Task ChargerAsync() => RafraichirAsync();

    [RelayCommand]
    protected async Task RafraichirAsync()
    {
        await ExecuterAsync(async () =>
        {
            var resultat = await LireAsync();

            Elements.Clear();
            foreach (var element in resultat.Elements)
            {
                Elements.Add(element);
            }

            NombreTotal = resultat.Total;
        });

        OnPropertyChanged(nameof(NombreTotal));
        OnPropertyChanged(nameof(NombrePages));
        OnPropertyChanged(nameof(AucunResultat));
        OnPropertyChanged(nameof(PagePrecedenteDisponible));
        OnPropertyChanged(nameof(PageSuivanteDisponible));
    }

    /// <summary>Relance la recherche depuis la première page.</summary>
    [RelayCommand]
    private async Task ChercherAsync()
    {
        Page = 1;
        await RafraichirAsync();
    }

    [RelayCommand]
    private async Task PagePrecedenteAsync()
    {
        if (!PagePrecedenteDisponible) return;

        Page--;
        await RafraichirAsync();
    }

    [RelayCommand]
    private async Task PageSuivanteAsync()
    {
        if (!PageSuivanteDisponible) return;

        Page++;
        await RafraichirAsync();
    }

    /// <summary>Redemande l'affichage des libellés après un changement de langue.</summary>
    protected virtual void RafraichirTextes()
    {
        OnPropertyChanged(nameof(Titre));
        OnPropertyChanged(nameof(Introduction));
        OnPropertyChanged(nameof(LibelleRechercher));
        OnPropertyChanged(nameof(LibelleActualiser));
        OnPropertyChanged(nameof(LibelleAjouter));
        OnPropertyChanged(nameof(LibelleModifier));
        OnPropertyChanged(nameof(LibelleSupprimer));
        OnPropertyChanged(nameof(LibelleAucunResultat));
    }
}
