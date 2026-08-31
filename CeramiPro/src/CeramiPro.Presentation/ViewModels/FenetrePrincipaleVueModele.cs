using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Fenêtre principale : le menu latéral, l'en-tête et la zone de contenu.
/// C'est elle qui décide des entrées visibles selon les droits de la personne
/// connectée, et qui bascule l'interface d'une langue à l'autre.
/// </summary>
public partial class FenetrePrincipaleVueModele : ObservableObject
{
    private readonly IServiceNavigation _navigation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceLangue _langue;

    public FenetrePrincipaleVueModele(
        IServiceNavigation navigation,
        IUtilisateurCourant utilisateurCourant,
        IServiceLangue langue)
    {
        _navigation = navigation;
        _utilisateurCourant = utilisateurCourant;
        _langue = langue;

        Menu = FiltrerSelonLesDroits(CatalogueNavigation.Construire(langue));

        _navigation.VueChangee += vue => VueCourante = vue;
        _langue.LangueChangee += AppliquerLangue;

        VueCourante = _navigation.VueCourante;
    }

    /// <summary>Entrées du menu que l'utilisateur a le droit de voir.</summary>
    public IReadOnlyList<ElementNavigation> Menu { get; }

    /// <summary>Langues proposées dans l'en-tête.</summary>
    public IReadOnlyList<Langue> Langues { get; } = new[] { Langue.Francais, Langue.Arabe };

    [ObservableProperty]
    private VueModeleBase? _vueCourante;

    [ObservableProperty]
    private bool _menuReduit;

    public Langue LangueCourante => _langue.LangueCourante;

    /// <summary>
    /// Sens de lecture. En arabe, toute la fenêtre s'inverse : le menu passe
    /// à droite, les colonnes et les champs suivent.
    /// </summary>
    public SensEcriture Sens => _langue.Sens;

    public string NomApplication => _langue["app.nom"];

    public string SousTitreApplication => _langue["app.sousTitre"];

    /// <summary>
    /// Tant que l'authentification n'existe pas, l'encadré du bas annonce
    /// clairement qu'aucune session n'est ouverte, plutôt qu'un tiret muet.
    /// </summary>
    public string NomUtilisateur
        => _utilisateurCourant.NomUtilisateur ?? _langue["message.sessionAbsente"];

    public string NomRole => _utilisateurCourant.CodeRole ?? string.Empty;

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

    [RelayCommand]
    private void ChoisirLangue(Langue langue) => _langue.Changer(langue);

    /// <summary>Rafraîchit tout ce qui dépend de la langue, sans redémarrer.</summary>
    private void AppliquerLangue()
    {
        foreach (var element in Menu)
        {
            element.RafraichirLibelle();
        }

        OnPropertyChanged(nameof(LangueCourante));
        OnPropertyChanged(nameof(Sens));
        OnPropertyChanged(nameof(NomApplication));
        OnPropertyChanged(nameof(SousTitreApplication));
        OnPropertyChanged(nameof(NomUtilisateur));
    }

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
                    _langue, element.CleLibelle, element.Icone,
                    element.Destination, element.DroitRequis, enfants));
            }
        }

        return visibles;
    }
}
