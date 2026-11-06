using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Un filtre affiché au-dessus d'un tableau. Comme les colonnes, les filtres
/// sont déclarés par l'écran : l'écran de liste générique les présente sans
/// rien savoir du métier, et relance la recherche dès qu'ils changent.
/// </summary>
public abstract class FiltreListe : ObservableObject
{
    private readonly Func<Task> _quandChange;

    protected FiltreListe(string libelle, Func<Task> quandChange)
    {
        Libelle = libelle;
        _quandChange = quandChange;
    }

    public string Libelle { get; }

    protected void Signaler() => _ = _quandChange();
}

/// <summary>Une case à cocher : « seulement le stock faible ».</summary>
public partial class FiltreCase : FiltreListe
{
    public FiltreCase(string libelle, Func<Task> quandChange, bool actif = false)
        : base(libelle, quandChange)
        => _actif = actif;

    [ObservableProperty]
    private bool _actif;

    partial void OnActifChanged(bool value) => Signaler();
}

/// <summary>Une liste de choix : « étape », « canal ».</summary>
public partial class FiltreChoix : FiltreListe
{
    public FiltreChoix(string libelle, IReadOnlyList<OptionChamp> options, Func<Task> quandChange, int valeur)
        : base(libelle, quandChange)
    {
        Options = options;
        _valeur = valeur;
    }

    public IReadOnlyList<OptionChamp> Options { get; }

    [ObservableProperty]
    private int _valeur;

    partial void OnValeurChanged(int value) => Signaler();
}
