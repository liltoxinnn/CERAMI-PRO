using CeramiPro.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Écran d'accueil. À cette étape il présente l'état réel du socle technique ;
/// les chiffres de l'atelier y seront branchés quand les ventes et la
/// production existeront. Aucun chiffre n'est inventé en attendant.
/// </summary>
public partial class TableauDeBordVueModele : VueModeleBase
{
    private readonly IServiceEtatBaseDeDonnees _verificationBase;

    public TableauDeBordVueModele(IServiceEtatBaseDeDonnees verificationBase)
        => _verificationBase = verificationBase;

    public override string Titre => "Tableau de bord";

    public override string Introduction =>
        "Vue d'ensemble de l'atelier : ventes, production, stock et alertes du jour.";

    /// <summary>État constaté de la base, affiché tel quel à l'écran.</summary>
    [ObservableProperty]
    private string _etatBaseDeDonnees = "Vérification en cours…";

    [ObservableProperty]
    private bool _baseDeDonneesDisponible;

    public override async Task ChargerAsync()
    {
        var etat = await _verificationBase.VerifierAsync();

        BaseDeDonneesDisponible = etat.Disponible;
        EtatBaseDeDonnees = etat.Message;
    }
}
