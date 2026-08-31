using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Écran d'accueil. À cette étape il présente la structure de l'atelier ;
/// les chiffres réels y seront branchés quand les modules correspondants
/// existeront — aucun chiffre inventé n'est affiché entre-temps.
/// </summary>
public partial class TableauDeBordVueModele : VueModeleBase
{
    public override string Titre => "Tableau de bord";

    public override string Introduction =>
        "Vue d'ensemble de l'atelier : ventes, production, stock et alertes du jour.";

    [ObservableProperty]
    private string _etatBaseDeDonnees = "Vérification…";

    [ObservableProperty]
    private bool _baseDeDonneesDisponible;
}
