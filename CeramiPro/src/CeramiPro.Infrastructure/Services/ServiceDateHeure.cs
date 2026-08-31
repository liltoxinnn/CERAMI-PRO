using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Horloge de l'atelier, réglée sur le fuseau d'Alger. Si le système ne
/// connaît pas ce fuseau, le logiciel continue de fonctionner en temps
/// universel plutôt que de refuser de démarrer.
/// </summary>
public class ServiceDateHeure : IServiceDateHeure
{
    private readonly TimeZoneInfo _fuseau;

    public ServiceDateHeure(ILogger<ServiceDateHeure> journal, string? fuseauHoraire = null)
    {
        var identifiant = string.IsNullOrWhiteSpace(fuseauHoraire)
            ? ParametresAtelier.FuseauHoraire
            : fuseauHoraire;

        try
        {
            _fuseau = TimeZoneInfo.FindSystemTimeZoneById(identifiant);
        }
        catch (Exception erreur) when (erreur is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            journal.LogWarning(
                "Le fuseau horaire « {Fuseau} » est inconnu de ce système. " +
                "Les heures seront affichées en temps universel.", identifiant);

            _fuseau = TimeZoneInfo.Utc;
        }
    }

    public DateTime MaintenantUtc => DateTime.UtcNow;

    public DateTime MaintenantAtelier => VersHeureAtelier(DateTime.UtcNow);

    public DateTime Aujourdhui => MaintenantAtelier.Date;

    public DateTime VersHeureAtelier(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(
            DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuseau);

    public DateTime VersUtc(DateTime heureAtelier)
        => TimeZoneInfo.ConvertTimeToUtc(
            DateTime.SpecifyKind(heureAtelier, DateTimeKind.Unspecified), _fuseau);
}
