using CeramiPro.Application.DTOs.Settings;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Paramètres de l'atelier : identité de l'entreprise, mentions légales,
/// devise, TVA et préfixes des numéros de documents.
///
/// Ces valeurs apparaissent sur les factures et les reçus : elles doivent
/// être renseignées avant la première vente.
/// </summary>
public partial class ParametresVueModele : VueModeleBase
{
    private readonly IParametresService _parametres;
    private readonly IServiceLangue _langue;
    private readonly IServiceDialogue _dialogue;

    public ParametresVueModele(
        IParametresService parametres, IServiceLangue langue, IServiceDialogue dialogue)
    {
        _parametres = parametres;
        _langue = langue;
        _dialogue = dialogue;
    }

    public override string Titre => _langue["menu.parametres"];

    public override string Introduction =>
        "Identité de l'atelier, mentions légales et numérotation des documents. " +
        "Ces informations apparaissent en tête de chaque facture.";

    /// <summary>Objet lié aux champs de l'écran.</summary>
    [ObservableProperty]
    private ParametresAtelierDto _reglages = new();

    [ObservableProperty]
    private bool _enregistrementEnCours;

    public override async Task ChargerAsync()
        => await ExecuterAsync(async () => Reglages = await _parametres.ObtenirAsync());

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (string.IsNullOrWhiteSpace(Reglages.NomAtelier))
        {
            MessageErreur = "Le nom de l'atelier est obligatoire : il figure sur toutes les factures.";
            return;
        }

        EnregistrementEnCours = true;

        await ExecuterAsync(async () =>
        {
            Reglages = await _parametres.ModifierAsync(Reglages);
            _dialogue.Succes("Les paramètres de l'atelier ont été enregistrés.");
        });

        EnregistrementEnCours = false;

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Rétablit les valeurs enregistrées, en annulant les saisies en cours.</summary>
    [RelayCommand]
    private async Task AnnulerAsync()
    {
        if (!_dialogue.Confirmer("Abandonner les modifications non enregistrées ?"))
        {
            return;
        }

        await ChargerAsync();
    }
}
