using System.Collections.ObjectModel;
using CeramiPro.Application.DTOs.Sauvegarde;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Sauvegarde des données de l'atelier.
///
/// L'archive contient une copie lisible de chaque table : elle peut être
/// relue sans le logiciel, ce qui est la première qualité d'une sauvegarde.
/// </summary>
public partial class SauvegardeVueModele : VueModeleBase
{
    private readonly ISauvegardeService _sauvegardes;
    private readonly IServiceLangue _langue;
    private readonly IServiceDialogue _dialogue;
    private readonly IServiceFichier _fichiers;

    public SauvegardeVueModele(
        ISauvegardeService sauvegardes,
        IServiceLangue langue,
        IServiceDialogue dialogue,
        IServiceFichier fichiers)
    {
        _sauvegardes = sauvegardes;
        _langue = langue;
        _dialogue = dialogue;
        _fichiers = fichiers;
    }

    public override string Titre => _langue["menu.administration.sauvegarde"];

    public override string Introduction =>
        "Copie de sécurité des données de l'atelier. Copiez régulièrement la dernière archive " +
        "sur une clé USB : une sauvegarde restée sur le même ordinateur ne protège de rien.";

    public ObservableCollection<SauvegardeDto> Sauvegardes { get; } = new();

    [ObservableProperty]
    private bool _automatiqueActive;

    [ObservableProperty]
    private string _heureAutomatique = string.Empty;

    [ObservableProperty]
    private int _conservationJours;

    [ObservableProperty]
    private string _dossier = string.Empty;

    [ObservableProperty]
    private string _nomBaseDeDonnees = string.Empty;

    [ObservableProperty]
    private DateTime? _derniereSauvegarde;

    [ObservableProperty]
    private SauvegardeDto? _sauvegardeSelectionnee;

    public bool AucuneSauvegarde => !ChargementEnCours && Sauvegardes.Count == 0;

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var etat = await _sauvegardes.EtatAsync();

            AutomatiqueActive = etat.AutomatiqueActive;
            HeureAutomatique = etat.HeureAutomatique;
            ConservationJours = etat.ConservationJours;
            Dossier = etat.Dossier;
            NomBaseDeDonnees = etat.NomBaseDeDonnees;
            DerniereSauvegarde = etat.DerniereSauvegarde;

            Sauvegardes.Clear();
            foreach (var sauvegarde in etat.Sauvegardes)
            {
                Sauvegardes.Add(sauvegarde);
            }
        });

        OnPropertyChanged(nameof(AucuneSauvegarde));
    }

    /// <summary>Crée une sauvegarde immédiatement.</summary>
    [RelayCommand]
    private async Task SauvegarderAsync()
    {
        await ExecuterAsync(async () =>
        {
            var sauvegarde = await _sauvegardes.CreerAsync();

            await ChargerAsync();

            _dialogue.Succes(
                $"La sauvegarde « {sauvegarde.NomFichier} » a été créée ({sauvegarde.TailleAffichee}).\n\n" +
                "Copiez-la sur une clé USB ou un disque externe.");
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Copie la sauvegarde choisie à l'endroit voulu par l'utilisateur.</summary>
    [RelayCommand]
    private async Task CopierAsync()
    {
        if (SauvegardeSelectionnee is not { } choisie)
        {
            _dialogue.Avertissement("Choisissez d'abord une sauvegarde dans la liste.");
            return;
        }

        await ExecuterAsync(async () =>
        {
            var (nomFichier, contenu) = await _sauvegardes.TelechargerAsync(choisie.NomFichier);

            if (_fichiers.DemanderOuEnregistrer(nomFichier, "Archive ZIP (*.zip)|*.zip") is not { } chemin)
            {
                return;
            }

            await File.WriteAllBytesAsync(chemin, contenu);

            _dialogue.Succes($"La sauvegarde a été copiée :\n{chemin}");
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    [RelayCommand]
    private async Task SupprimerAsync()
    {
        if (SauvegardeSelectionnee is not { } choisie)
        {
            _dialogue.Avertissement("Choisissez d'abord une sauvegarde dans la liste.");
            return;
        }

        if (!_dialogue.Confirmer(
                $"Supprimer définitivement la sauvegarde « {choisie.NomFichier} » ?\n\n" +
                "Cette archive ne pourra plus être récupérée."))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _sauvegardes.SupprimerAsync(choisie.NomFichier);
            await ChargerAsync();
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Supprime les archives qui dépassent la durée de conservation.</summary>
    [RelayCommand]
    private async Task PurgerAsync()
    {
        if (!_dialogue.Confirmer(
                $"Supprimer les sauvegardes de plus de {ConservationJours} jours ?"))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            var supprimees = await _sauvegardes.PurgerAsync();

            await ChargerAsync();

            _dialogue.Information(supprimees == 0
                ? "Aucune sauvegarde n'était assez ancienne pour être supprimée."
                : $"{supprimees} sauvegarde(s) ancienne(s) ont été supprimées.");
        });
    }

    [RelayCommand]
    private void OuvrirDossier()
    {
        if (!string.IsNullOrWhiteSpace(Dossier))
        {
            _fichiers.Ouvrir(Dossier);
        }
    }
}
