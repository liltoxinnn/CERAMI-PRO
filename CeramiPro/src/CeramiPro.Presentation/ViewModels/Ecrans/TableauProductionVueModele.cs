using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Tableau de production : les ordres rangés par étape de fabrication, du
/// façonnage à l'emballage.
///
/// C'est la vue d'atelier : on y voit d'un coup d'œil où en est chaque
/// fabrication, et on fait avancer une pièce d'une étape à la suivante.
/// </summary>
public partial class TableauProductionVueModele : VueModeleBase
{
    private readonly IProductionService _productions;
    private readonly IServiceLangue _langue;
    private readonly IServiceDialogue _dialogue;

    public TableauProductionVueModele(
        IProductionService productions, IServiceLangue langue, IServiceDialogue dialogue)
    {
        _productions = productions;
        _langue = langue;
        _dialogue = dialogue;
    }

    public override string Titre => _langue["menu.production.tableau"];

    public override string Introduction =>
        "Où en est chaque fabrication. Choisissez un ordre, puis faites-le avancer à l'étape suivante.";

    /// <summary>Une colonne par étape de fabrication.</summary>
    public ObservableCollection<ColonneProductionDto> Colonnes { get; } = new();

    [ObservableProperty]
    private OrdreProductionDto? _ordreSelectionne;

    [ObservableProperty]
    private decimal _quantiteAcceptee;

    [ObservableProperty]
    private decimal _quantiteEndommagee;

    [ObservableProperty]
    private string? _notes;

    public bool AucunOrdre => !ChargementEnCours && Colonnes.All(c => c.Ordres.Count == 0);

    /// <summary>Étape qui suit celle de l'ordre choisi ; nulle si la fabrication est finie.</summary>
    public ProductionStatus? EtapeSuivante => OrdreSelectionne is { } ordre
        ? EtapeApres(ordre.Statut)
        : null;

    public string LibelleEtapeSuivante => EtapeSuivante is { } etape
        ? "Passer à « " + etape.Libelle() + " »"
        : "Aucune étape suivante";

    public bool PeutAvancer => EtapeSuivante is not null;

    /// <summary>Vrai tant que les matières n'ont pas été sorties du stock.</summary>
    public bool PeutLancer => OrdreSelectionne is { MatieresConsommees: false };

    partial void OnOrdreSelectionneChanged(OrdreProductionDto? value)
    {
        // Proposer la quantité prévue évite de la retaper à chaque étape ;
        // elle reste modifiable en cas de casse.
        QuantiteAcceptee = value?.QuantitePrevue ?? 0m;
        QuantiteEndommagee = 0m;
        Notes = null;

        OnPropertyChanged(nameof(EtapeSuivante));
        OnPropertyChanged(nameof(LibelleEtapeSuivante));
        OnPropertyChanged(nameof(PeutAvancer));
        OnPropertyChanged(nameof(PeutLancer));
    }

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var colonnes = await _productions.TableauAsync();

            Colonnes.Clear();
            foreach (var colonne in colonnes)
            {
                Colonnes.Add(colonne);
            }
        });

        OnPropertyChanged(nameof(AucunOrdre));
    }

    [RelayCommand]
    private Task ActualiserAsync() => ChargerAsync();

    /// <summary>Retient l'ordre sur lequel on vient de cliquer dans le tableau.</summary>
    [RelayCommand]
    private void Choisir(OrdreProductionDto? ordre)
    {
        OrdreSelectionne = ordre;
        MessageErreur = null;
    }

    /// <summary>Sort les matières du stock et ouvre la fabrication.</summary>
    [RelayCommand]
    private async Task LancerAsync()
    {
        if (OrdreSelectionne is not { } ordre)
        {
            _dialogue.Avertissement("Choisissez d'abord un ordre de production.");
            return;
        }

        if (!_dialogue.Confirmer(
                $"Lancer la fabrication de l'ordre {ordre.Numero} ?\n\n" +
                "Les matières prévues par la recette vont sortir du stock."))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _productions.LancerAsync(ordre.Id, new LancementProductionRequete());
            await ChargerAsync();
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Fait passer l'ordre choisi à l'étape suivante.</summary>
    [RelayCommand]
    private async Task AvancerAsync()
    {
        if (OrdreSelectionne is not { } ordre)
        {
            _dialogue.Avertissement("Choisissez d'abord un ordre de production.");
            return;
        }

        if (EtapeSuivante is not { } etape)
        {
            _dialogue.Information("Cette fabrication est déjà terminée.");
            return;
        }

        if (QuantiteAcceptee + QuantiteEndommagee <= 0m)
        {
            MessageErreur = "Indiquez au moins une pièce acceptée ou cassée.";
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _productions.ChangerEtapeAsync(ordre.Id, new ChangementEtapeRequete
            {
                NouvelleEtape = etape,
                QuantiteAcceptee = QuantiteAcceptee,
                QuantiteEndommagee = QuantiteEndommagee,
                Notes = Notes
            });

            OrdreSelectionne = null;
            await ChargerAsync();
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    [RelayCommand]
    private async Task AnnulerOrdreAsync()
    {
        if (OrdreSelectionne is not { } ordre)
        {
            _dialogue.Avertissement("Choisissez d'abord un ordre de production.");
            return;
        }

        if (!_dialogue.Confirmer(
                $"Annuler l'ordre {ordre.Numero} ?\n\n" +
                "Les matières déjà consommées seront remises en stock."))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _productions.AnnulerAsync(ordre.Id, "Annulé depuis le tableau de production.");

            OrdreSelectionne = null;
            await ChargerAsync();
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>
    /// Ordre des étapes de fabrication. La suite se déduit de la valeur
    /// numérique de l'étape : le tableau et le service suivent ainsi le même
    /// enchaînement, sans risque de divergence.
    /// </summary>
    public static ProductionStatus? EtapeApres(ProductionStatus etape)
        => Enchainement.SkipWhile(e => e != etape).Skip(1).Cast<ProductionStatus?>().FirstOrDefault();

    private static readonly IReadOnlyList<ProductionStatus> Enchainement =
        Enum.GetValues<ProductionStatus>()
            .Where(e => e != ProductionStatus.Annule)
            .OrderBy(e => (int)e)
            .ToList();
}
