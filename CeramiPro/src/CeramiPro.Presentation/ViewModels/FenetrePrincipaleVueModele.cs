using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Fenêtre principale : le menu latéral, l'en-tête et la zone de contenu.
/// C'est elle qui décide des entrées visibles selon le rôle de la personne
/// connectée.
/// </summary>
public partial class FenetrePrincipaleVueModele : ObservableObject
{
    private readonly IServiceNavigation _navigation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceCompte? _compte;

    public FenetrePrincipaleVueModele(
        IServiceNavigation navigation,
        IUtilisateurCourant utilisateurCourant,
        IServiceCompte? compte = null)
    {
        _navigation = navigation;
        _utilisateurCourant = utilisateurCourant;
        _compte = compte;

        Menu = FiltrerSelonLeRole(CatalogueNavigation.Construire());

        _navigation.VueChangee += vue => VueCourante = vue;

        VueCourante = _navigation.VueCourante;
    }

    /// <summary>Entrées du menu que l'utilisateur peut ouvrir.</summary>
    public IReadOnlyList<ElementNavigation> Menu { get; }

    [ObservableProperty]
    private VueModeleBase? _vueCourante;

    [ObservableProperty]
    private bool _menuReduit;

    public string NomApplication => "CeramiPro";

    public string SousTitreApplication => "Gestion d'atelier céramique";

    /// <summary>
    /// Sans session ouverte, l'encadré du bas l'annonce clairement plutôt
    /// que d'afficher un tiret muet.
    /// </summary>
    public string NomUtilisateur
        => _utilisateurCourant.NomComplet
           ?? _utilisateurCourant.NomUtilisateur
           ?? "Aucune session";

    /// <summary>Nom lisible du rôle, jamais son code technique.</summary>
    public string NomRole => _utilisateurCourant.NomRole ?? string.Empty;

    public string LibelleDeconnexion => "Se déconnecter";

    public string LibelleChangerMotDePasse => "Changer mon mot de passe";

    /// <summary>
    /// Ouvre la fenêtre de changement de mot de passe. Chacun doit pouvoir
    /// changer le sien sans passer par un administrateur.
    /// </summary>
    [RelayCommand]
    private void ChangerMotDePasse() => _compte?.ChangerMotDePasse();

    /// <summary>
    /// Ferme la session et rend la main à l'écran de connexion : c'est ce
    /// qu'il faut quand deux personnes se relaient au comptoir.
    /// </summary>
    [RelayCommand]
    private void Deconnecter() => _compte?.Deconnecter();

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
    /// Retire les entrées réservées aux administrateurs. Un groupe dont toutes
    /// les sous-entrées sont retirées disparaît lui aussi, plutôt que de
    /// rester vide.
    /// </summary>
    private IReadOnlyList<ElementNavigation> FiltrerSelonLeRole(
        IReadOnlyList<ElementNavigation> elements)
    {
        var visibles = new List<ElementNavigation>();

        foreach (var element in elements)
        {
            if (element.AdministrateurSeulement && !_utilisateurCourant.EstAdministrateur)
            {
                continue;
            }

            if (!element.EstGroupe)
            {
                visibles.Add(element);
                continue;
            }

            var enfants = FiltrerSelonLeRole(element.Enfants);

            if (enfants.Count > 0)
            {
                visibles.Add(new ElementNavigation(
                    element.Libelle, element.Icone, element.Destination,
                    element.AdministrateurSeulement, enfants));
            }
        }

        return visibles;
    }
}
