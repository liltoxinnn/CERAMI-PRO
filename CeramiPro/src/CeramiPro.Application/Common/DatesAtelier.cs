using CeramiPro.Application.Interfaces;

namespace CeramiPro.Application.Common;

/// <summary>
/// Passage entre les dates saisies à l'écran — des jours de l'atelier, à
/// l'heure d'Alger — et les instants enregistrés en base, en UTC.
///
/// Une date choisie dans un calendrier n'a pas d'heure ; laissée telle
/// quelle, elle serait convertie avec le fuseau de l'ordinateur et pourrait
/// glisser d'un jour. Les services la convertissent donc ici, avec le fuseau
/// de l'atelier, et les écrans relisent toujours des heures locales.
/// </summary>
public static class DatesAtelier
{
    /// <summary>Minuit, au début de ce jour de l'atelier, en UTC.</summary>
    public static DateTime DebutDuJourUtc(this IServiceDateHeure horloge, DateTime jour)
        => DateTime.SpecifyKind(horloge.VersUtc(jour.Date), DateTimeKind.Utc);

    /// <summary>
    /// Instant à enregistrer pour un jour saisi : l'instant présent s'il
    /// s'agit d'aujourd'hui (ou si rien n'est saisi), midi sinon — loin de
    /// minuit, la journée ne peut plus changer.
    /// </summary>
    public static DateTime JourSaisiVersUtc(this IServiceDateHeure horloge, DateTime? jour)
        => jour is not { } choisi || choisi.Date == horloge.Aujourdhui
            ? horloge.MaintenantUtc
            : DateTime.SpecifyKind(horloge.VersUtc(choisi.Date.AddHours(12)), DateTimeKind.Utc);

    /// <summary>Instant enregistré, ramené à l'heure de l'atelier pour l'affichage.</summary>
    public static DateTime EnHeureAtelier(this IServiceDateHeure horloge, DateTime utc)
        => horloge.VersHeureAtelier(utc);

    public static DateTime? EnHeureAtelier(this IServiceDateHeure horloge, DateTime? utc)
        => utc is { } valeur ? horloge.VersHeureAtelier(valeur) : null;
}
