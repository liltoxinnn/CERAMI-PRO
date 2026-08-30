namespace CeramicWorkshop.Application.Interfaces;

/// <summary>
/// Horloge de l'application. Les dates sont conservées en UTC dans PostgreSQL
/// et converties vers le fuseau de l'atelier (Africa/Algiers) pour l'affichage.
/// </summary>
public interface IDateTimeService
{
    DateTime UtcNow { get; }

    /// <summary>Date et heure locales de l'atelier.</summary>
    DateTime MaintenantAtelier { get; }

    /// <summary>Date du jour de l'atelier, à minuit.</summary>
    DateTime AujourdHui { get; }

    DateTime VersHeureAtelier(DateTime utc);
    DateTime VersUtc(DateTime heureAtelier);
}
