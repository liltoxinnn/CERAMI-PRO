using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Sauvegarde automatique quotidienne.
///
/// Le service se réveille toutes les dix minutes, compare l'heure de l'atelier
/// à l'heure choisie dans les paramètres, et crée une archive si aucune n'a
/// encore été faite dans la journée. Les archives trop anciennes sont ensuite
/// supprimées selon la durée de conservation configurée.
///
/// Rien n'est fait tant que le réglage « sauvegarde.automatique » est désactivé.
/// </summary>
public class SauvegardeAutomatique : BackgroundService
{
    /// <summary>Intervalle entre deux vérifications.</summary>
    public static readonly TimeSpan Intervalle = TimeSpan.FromMinutes(10);

    private readonly IServiceScopeFactory _fabrique;
    private readonly ILogger<SauvegardeAutomatique> _journal;

    private DateTime? _derniereJournee;

    public SauvegardeAutomatique(IServiceScopeFactory fabrique, ILogger<SauvegardeAutomatique> journal)
    {
        _fabrique = fabrique;
        _journal = journal;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var minuterie = new PeriodicTimer(Intervalle);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await VerifierAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception erreur)
            {
                // Une sauvegarde ratée ne doit jamais arrêter le logiciel.
                _journal.LogError(erreur, "La sauvegarde automatique a échoué.");
            }

            if (!await minuterie.WaitForNextTickAsync(stoppingToken))
            {
                break;
            }
        }
    }

    private async Task VerifierAsync(CancellationToken cancellationToken)
    {
        using var portee = _fabrique.CreateScope();

        var contexte = portee.ServiceProvider.GetRequiredService<CeramiProDbContext>();
        var horloge = portee.ServiceProvider.GetRequiredService<IServiceDateHeure>();

        if (!await contexte.Database.CanConnectAsync(cancellationToken))
        {
            return;
        }

        var reglages = await contexte.SystemSettings.AsNoTracking()
            .Where(s => s.Key == SauvegardeService.CleAutomatique || s.Key == SauvegardeService.CleHeure)
            .ToDictionaryAsync(s => s.Key, s => s.Value, cancellationToken);

        var active = reglages.TryGetValue(SauvegardeService.CleAutomatique, out var valeur)
                     && bool.TryParse(valeur, out var oui) && oui;

        if (!active)
        {
            return;
        }

        var heureTexte = reglages.TryGetValue(SauvegardeService.CleHeure, out var texte) ? texte : "22:00";

        if (!TimeSpan.TryParse(heureTexte, out var heure))
        {
            heure = new TimeSpan(22, 0, 0);
        }

        var maintenant = horloge.MaintenantAtelier;

        if (_derniereJournee == maintenant.Date || maintenant.TimeOfDay < heure)
        {
            return;
        }

        var sauvegardes = portee.ServiceProvider.GetRequiredService<ISauvegardeService>();

        await sauvegardes.CreerAsync(automatique: true, cancellationToken);
        await sauvegardes.PurgerAsync(cancellationToken);

        _derniereJournee = maintenant.Date;
    }
}
