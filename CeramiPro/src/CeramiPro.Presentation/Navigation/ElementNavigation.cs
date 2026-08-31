using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Une entrée du menu latéral. Un élément sans <see cref="Destination"/> mais
/// avec des enfants est un groupe : il se déplie au clic.
/// </summary>
public partial class ElementNavigation : ObservableObject
{
    public ElementNavigation(
        string libelle,
        string icone,
        Type? destination = null,
        string? droitRequis = null,
        IReadOnlyList<ElementNavigation>? enfants = null)
    {
        Libelle = libelle;
        Icone = icone;
        Destination = destination;
        DroitRequis = droitRequis;
        Enfants = enfants ?? Array.Empty<ElementNavigation>();
    }

    /// <summary>Texte affiché, en français.</summary>
    public string Libelle { get; }

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
}
