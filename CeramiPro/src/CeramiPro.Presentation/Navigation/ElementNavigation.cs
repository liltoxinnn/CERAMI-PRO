namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Une entrée du menu latéral. Un élément sans <see cref="Destination"/> est
/// un simple regroupement : il déplie ses sous-entrées.
/// </summary>
public class ElementNavigation
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
}
