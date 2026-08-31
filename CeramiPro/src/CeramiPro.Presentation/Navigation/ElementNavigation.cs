using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Une entrée du menu latéral.
///
/// L'entrée ne retient qu'une clé de traduction, jamais un texte figé : c'est
/// ce qui permet de basculer l'interface en arabe sans reconstruire le menu.
/// </summary>
public partial class ElementNavigation : ObservableObject
{
    private readonly IServiceLangue _langue;

    public ElementNavigation(
        IServiceLangue langue,
        string cleLibelle,
        string icone,
        Type? destination = null,
        string? droitRequis = null,
        IReadOnlyList<ElementNavigation>? enfants = null)
    {
        _langue = langue;
        CleLibelle = cleLibelle;
        Icone = icone;
        Destination = destination;
        DroitRequis = droitRequis;
        Enfants = enfants ?? Array.Empty<ElementNavigation>();
    }

    /// <summary>Clé de traduction, par exemple « menu.stock ».</summary>
    public string CleLibelle { get; }

    /// <summary>Texte affiché, dans la langue courante.</summary>
    public string Libelle => _langue[CleLibelle];

    public string Icone { get; }

    /// <summary>Vue-modèle ouverte au clic.</summary>
    public Type? Destination { get; }

    /// <summary>Droit nécessaire pour voir cette entrée ; nul si accessible à tous.</summary>
    public string? DroitRequis { get; }

    public IReadOnlyList<ElementNavigation> Enfants { get; }

    public bool EstGroupe => Enfants.Count > 0;

    /// <summary>
    /// Un groupe est replié au départ : le menu tient à l'écran sans avoir à
    /// faire défiler seize rubriques.
    /// </summary>
    [ObservableProperty]
    private bool _estDeplie;

    /// <summary>Redemande l'affichage du libellé après un changement de langue.</summary>
    public void RafraichirLibelle()
    {
        OnPropertyChanged(nameof(Libelle));

        foreach (var enfant in Enfants)
        {
            enfant.RafraichirLibelle();
        }
    }
}
