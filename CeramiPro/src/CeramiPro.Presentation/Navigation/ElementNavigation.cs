using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.Navigation;

/// <summary>Une entrée du menu latéral.</summary>
public partial class ElementNavigation : ObservableObject
{
    public ElementNavigation(
        string libelle,
        string icone,
        Type? destination = null,
        bool administrateurSeulement = false,
        IReadOnlyList<ElementNavigation>? enfants = null)
    {
        Libelle = libelle;
        Icone = icone;
        Destination = destination;
        AdministrateurSeulement = administrateurSeulement;
        Enfants = enfants ?? Array.Empty<ElementNavigation>();
    }

    public string Libelle { get; }

    public string Icone { get; }

    /// <summary>Vue-modèle ouverte au clic ; nulle pour un groupe.</summary>
    public Type? Destination { get; }

    /// <summary>
    /// Entrée réservée aux administrateurs : comptes et sauvegardes. Le reste
    /// du logiciel est ouvert à tous les employés de l'atelier.
    /// </summary>
    public bool AdministrateurSeulement { get; }

    public IReadOnlyList<ElementNavigation> Enfants { get; }

    public bool EstGroupe => Enfants.Count > 0;

    /// <summary>Un groupe est replié au départ, pour que le menu reste court.</summary>
    [ObservableProperty]
    private bool _estDeplie;
}
