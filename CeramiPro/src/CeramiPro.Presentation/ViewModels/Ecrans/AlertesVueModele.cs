using System.Collections.ObjectModel;
using CeramiPro.Application.DTOs.Alertes;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Centre d'alertes : stock faible, échéances de commandes, retards de
/// production, dettes en attente.
///
/// Les alertes sont recalculées à chaque affichage : elles reflètent l'état
/// réel de l'atelier, et non une liste figée au démarrage.
/// </summary>
public partial class AlertesVueModele : VueModeleBase
{
    private readonly IAlerteService _alertes;
    private readonly IServiceLangue _langue;
    private readonly IServiceDialogue _dialogue;

    public AlertesVueModele(
        IAlerteService alertes, IServiceLangue langue, IServiceDialogue dialogue)
    {
        _alertes = alertes;
        _langue = langue;
        _dialogue = dialogue;
    }

    public override string Titre => _langue["menu.stock.alertes"];

    public override string Introduction =>
        "Ce qui demande votre attention aujourd'hui : stock au plus bas, commandes proches de " +
        "l'échéance, productions en retard et sommes restant à encaisser.";

    public ObservableCollection<AlerteDto> Alertes { get; } = new();

    [ObservableProperty]
    private bool _seulementNonLues = true;

    [ObservableProperty]
    private int _total;

    [ObservableProperty]
    private int _nonLues;

    [ObservableProperty]
    private int _critiques;

    public bool AucuneAlerte => !ChargementEnCours && Alertes.Count == 0;

    partial void OnSeulementNonLuesChanged(bool value) => _ = ChargerAsync();

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var liste = await _alertes.ListerAsync(
                new FiltreAlertesRequete { SeulementNonLues = SeulementNonLues });

            Alertes.Clear();
            foreach (var alerte in liste)
            {
                Alertes.Add(alerte);
            }

            var resume = await _alertes.ResumeAsync();

            Total = resume.Total;
            NonLues = resume.NonLues;
            Critiques = resume.Critiques;
        });

        OnPropertyChanged(nameof(AucuneAlerte));
    }

    [RelayCommand]
    private Task ActualiserAsync() => ChargerAsync();

    /// <summary>Marque une alerte comme lue sans la faire disparaître de la liste.</summary>
    [RelayCommand]
    private async Task MarquerLueAsync(AlerteDto? alerte)
    {
        if (alerte is null || alerte.Lue)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _alertes.MarquerLueAsync(alerte.Id);
            await ChargerAsync();
        });
    }

    [RelayCommand]
    private async Task ToutMarquerLuAsync()
    {
        if (NonLues == 0)
        {
            _dialogue.Information("Toutes les alertes ont déjà été lues.");
            return;
        }

        if (!_dialogue.Confirmer($"Marquer les {NonLues} alerte(s) non lue(s) comme lues ?"))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _alertes.ToutMarquerLuAsync();
            await ChargerAsync();
        });
    }

    /// <summary>Couleur d'une alerte selon sa gravité, jamais employée seule.</summary>
    public static string Teinte(NotificationSeverity gravite) => gravite switch
    {
        NotificationSeverity.Critique => "#B3261E",
        NotificationSeverity.Avertissement => "#8A5B00",
        _ => "#1C5D99"
    };
}
