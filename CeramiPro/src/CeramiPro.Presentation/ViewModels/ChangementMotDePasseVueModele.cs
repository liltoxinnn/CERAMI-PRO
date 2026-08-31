using CeramiPro.Application.DTOs.Auth;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Changement de mot de passe par la personne elle-même.
///
/// L'écran s'impose à la première connexion d'un compte créé par un
/// administrateur : sans cela, le mot de passe provisoire — communiqué de
/// vive voix ou par téléphone — resterait en usage indéfiniment.
/// </summary>
public partial class ChangementMotDePasseVueModele : VueModeleBase
{
    private readonly IAuthService _auth;
    private readonly IServiceLangue _langue;

    public ChangementMotDePasseVueModele(IAuthService auth, IServiceLangue langue)
    {
        _auth = auth;
        _langue = langue;
    }

    public override string Titre => "Changer le mot de passe";

    public override string Introduction => Obligatoire
        ? "Ce compte utilise encore un mot de passe provisoire. Choisissez-en un nouveau "
          + "avant d'ouvrir l'atelier."
        : "Choisissez un nouveau mot de passe.";

    /// <summary>
    /// Vrai quand le changement est imposé : la fenêtre ne peut alors pas
    /// être refermée sans avoir choisi un nouveau mot de passe.
    /// </summary>
    [ObservableProperty]
    private bool _obligatoire;

    [ObservableProperty]
    private string _motDePasseActuel = string.Empty;

    [ObservableProperty]
    private string _nouveauMotDePasse = string.Empty;

    [ObservableProperty]
    private string _confirmation = string.Empty;

    /// <summary>Vrai quand le changement a réussi : la fenêtre peut se fermer.</summary>
    [ObservableProperty]
    private bool _change;

    public string LibelleValider => _langue["action.valider"];

    public string LibelleAnnuler => _langue["action.annuler"];

    /// <summary>Rappel des règles, affiché avant la saisie plutôt qu'après l'échec.</summary>
    public string Exigences =>
        "Au moins huit caractères, dont une majuscule, une minuscule et un chiffre.";

    [RelayCommand]
    private async Task ValiderAsync()
    {
        MessageErreur = Verifier();

        if (MessageErreur is not null)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _auth.ChangerMotDePasseAsync(new ChangementMotDePasseRequete
            {
                MotDePasseActuel = MotDePasseActuel,
                NouveauMotDePasse = NouveauMotDePasse,
                ConfirmationMotDePasse = Confirmation
            });

            Change = true;
        });

        // Aucun mot de passe ne survit à la tentative, réussie ou non.
        MotDePasseActuel = string.Empty;
        NouveauMotDePasse = string.Empty;
        Confirmation = string.Empty;
    }

    /// <summary>
    /// Contrôles de saisie, faits ici pour éviter un aller-retour jusqu'à la
    /// base. Les règles de fond restent celles du service.
    /// </summary>
    private string? Verifier()
    {
        if (string.IsNullOrWhiteSpace(MotDePasseActuel))
        {
            return "Indiquez votre mot de passe actuel.";
        }

        if (string.IsNullOrWhiteSpace(NouveauMotDePasse))
        {
            return "Indiquez le nouveau mot de passe.";
        }

        if (NouveauMotDePasse != Confirmation)
        {
            return "Les deux saisies du nouveau mot de passe ne correspondent pas.";
        }

        return NouveauMotDePasse == MotDePasseActuel
            ? "Le nouveau mot de passe doit être différent de l'ancien."
            : null;
    }
}
