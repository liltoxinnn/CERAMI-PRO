using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Fenêtre principale : le menu latéral, l'en-tête et la zone de contenu.
/// C'est elle qui décide des entrées visibles selon les droits de la personne
/// connectée.
/// </summary>
public partial class FenetrePrincipaleVueModele : ObservableObject
{
    private readonly IServiceNavigation _navigation;
    private readonly IUtilisateurCourant _utilisateurCourant;

    public FenetrePrincipaleVueModele(
        IServiceNavigation navigation, IUtilisateurCourant utilisateurCourant)
    {
        _navigation = navigation;
        _utilisateurCourant = utilisateurCourant;

        Menu = FiltrerSelonLesDroits(CatalogueNavigation.Construire());

        _navigation.VueChangee += vue => VueCourante = vue;
        VueCourante = _navigation.VueCourante;
    }

    /// <summary>Entrées du menu que l'utilisateur a le droit de voir.</summary>
    public IReadOnlyList<ElementNavigation> Menu { get; }

    [ObservableProperty]
    private VueModeleBase? _vueCourante;

    [ObservableProperty]
    private bool _menuReduit;

    /// <summary>
    /// Tant que l'authentification n'existe pas, l'encadré du bas annonce
    /// clairement qu'aucune session n'est ouverte, plutôt qu'un tiret muet.
    /// </summary>
    public string NomUtilisateur => _utilisateurCourant.NomUtilisateur ?? "Aucune session";

    public string NomRole => _utilisateurCourant.CodeRole ?? "Connexion à l'étape 2";

    /// <summary>
    /// Un clic ouvre l'écran d'une entrée simple, ou déplie un groupe. Aucun
    /// élément du menu n'est ainsi sans effet.
    /// </summary>
    [RelayCommand]
    private void Naviguer(ElementNavigation element)
    {
        if (element.EstGroupe)
        {
            element.EstDeplie = !element.EstDeplie;
            return;
        }

        if (element.Destination is not null)
        {
            _navigation.Naviguer(element.Destination);
        }
    }

    [RelayCommand]
    private void BasculerMenu() => MenuReduit = !MenuReduit;

    /// <summary>
    /// Retire les entrées interdites. Un groupe dont toutes les sous-entrées
    /// sont interdites disparaît lui aussi, plutôt que de rester vide.
    /// </summary>
    private IReadOnlyList<ElementNavigation> FiltrerSelonLesDroits(
        IReadOnlyList<ElementNavigation> elements)
    {
        var visibles = new List<ElementNavigation>();

        foreach (var element in elements)
        {
            if (element.DroitRequis is not null && !_utilisateurCourant.PossedeDroit(element.DroitRequis))
            {
                continue;
            }

            if (!element.EstGroupe)
            {
                visibles.Add(element);
                continue;
            }

            var enfants = FiltrerSelonLesDroits(element.Enfants);

            if (enfants.Count > 0)
            {
                visibles.Add(new ElementNavigation(
                    element.Libelle, element.Icone, element.Destination, element.DroitRequis, enfants));
            }
        }

        return visibles;
    }
}
