using CeramiPro.Application.DTOs.Settings;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Paramètres de l'atelier : identité de l'entreprise, mentions légales,
/// devise, TVA, numérotation des documents, et état de la base de données.
///
/// L'identité et les mentions légales apparaissent sur les reçus : elles
/// doivent être renseignées avant la première vente.
/// </summary>
public partial class ParametresVueModele : VueModeleBase
{
    private readonly IParametresService _parametres;
    private readonly IServiceEtatBaseDeDonnees _base;
    private readonly IServiceDialogue _dialogue;

    public ParametresVueModele(
        IParametresService parametres, IServiceEtatBaseDeDonnees baseDeDonnees, IServiceDialogue dialogue)
    {
        _parametres = parametres;
        _base = baseDeDonnees;
        _dialogue = dialogue;
    }

    public override string Titre => "Paramètres de l'atelier";

    public override string Introduction =>
        "Identité de l'atelier, mentions légales, monnaie et numérotation des documents. " +
        "Le nom, l'adresse et les mentions légales apparaissent sur chaque reçu.";

    /// <summary>Connexion et version de la base, en lecture seule.</summary>
    [ObservableProperty]
    private InformationsBaseDeDonnees? _baseDeDonnees;

    /// <summary>Texte affiché dans la carte « Base de données ».</summary>
    public string EtatBase => BaseDeDonnees switch
    {
        null => "Vérification en cours…",
        { Disponible: false } => "Injoignable — vérifiez que PostgreSQL est démarré.",
        { MisesAJourEnAttente: > 0 } infos =>
            $"Connectée — {infos.MisesAJourEnAttente} mise(s) à jour du schéma en attente : redémarrez le logiciel.",
        _ => "Connectée"
    };

    partial void OnBaseDeDonneesChanged(InformationsBaseDeDonnees? value) => OnPropertyChanged(nameof(EtatBase));

    /// <summary>Objet lié aux champs de l'écran.</summary>
    [ObservableProperty]
    private ParametresAtelierDto _reglages = new();

    [ObservableProperty]
    private bool _enregistrementEnCours;

    public override async Task ChargerAsync()
        => await ExecuterAsync(async () =>
        {
            Reglages = await _parametres.ObtenirAsync();
            BaseDeDonnees = await _base.DecrireAsync();
        });

    /// <summary>Revérifie la connexion, par exemple après avoir redémarré PostgreSQL.</summary>
    [RelayCommand]
    private async Task VerifierBaseAsync()
        => await ExecuterAsync(async () => BaseDeDonnees = await _base.DecrireAsync());

    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        if (string.IsNullOrWhiteSpace(Reglages.NomAtelier))
        {
            MessageErreur = "Le nom de l'atelier est obligatoire : il figure sur tous les reçus.";
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
