using CeramicWorkshop.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace CeramicWorkshop.Infrastructure.Services;

/// <summary>
/// Horloge de l'atelier. Les dates sont stockées en UTC et présentées
/// dans le fuseau horaire de l'atelier (Africa/Algiers par défaut).
/// </summary>
public class DateTimeService : IDateTimeService
{
    public const string FuseauParDefaut = "Africa/Algiers";

    private readonly TimeZoneInfo _fuseau;

    public DateTimeService(ILogger<DateTimeService> journal, string? identifiantFuseau = null)
    {
        var identifiant = string.IsNullOrWhiteSpace(identifiantFuseau) ? FuseauParDefaut : identifiantFuseau;

        try
        {
            _fuseau = TimeZoneInfo.FindSystemTimeZoneById(identifiant);
        }
        catch (Exception ex) when (ex is TimeZoneNotFoundException or InvalidTimeZoneException)
        {
            journal.LogWarning(ex,
                "Fuseau horaire « {Fuseau} » introuvable sur ce serveur : l'heure UTC est utilisée.", identifiant);
            _fuseau = TimeZoneInfo.Utc;
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime MaintenantAtelier => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _fuseau);

    public DateTime AujourdHui => MaintenantAtelier.Date;

    public DateTime VersHeureAtelier(DateTime utc)
        => TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), _fuseau);

    public DateTime VersUtc(DateTime heureAtelier)
        => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(heureAtelier, DateTimeKind.Unspecified), _fuseau);
}
