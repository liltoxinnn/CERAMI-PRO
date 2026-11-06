using CeramiPro.Application.Interfaces;

namespace CeramiPro.Application.Common;

/// <summary>
/// Une période exprimée en jours de l'atelier — à l'heure d'Alger — et ses
/// bornes en temps universel, pour interroger la base.
///
/// Sans cette conversion, une vente faite à minuit et demi serait comptée
/// la veille : la base enregistre en UTC, et Alger a une heure d'avance.
/// </summary>
public readonly record struct PeriodeAtelier(
    DateTime PremierJour, DateTime DernierJour, DateTime DebutUtc, DateTime FinUtc)
{
    /// <summary>Du premier au dernier jour inclus, à l'heure de l'atelier.</summary>
    public static PeriodeAtelier Jours(IServiceDateHeure horloge, DateTime premierJour, DateTime dernierJour)
    {
        var debut = premierJour.Date;
        var fin = dernierJour.Date < debut ? debut : dernierJour.Date;

        return new PeriodeAtelier(
            debut, fin,
            DateTime.SpecifyKind(horloge.VersUtc(debut), DateTimeKind.Utc),
            DateTime.SpecifyKind(horloge.VersUtc(fin.AddDays(1)), DateTimeKind.Utc));
    }

    /// <summary>Une seule journée de l'atelier.</summary>
    public static PeriodeAtelier Jour(IServiceDateHeure horloge, DateTime jour)
        => Jours(horloge, jour, jour);

    /// <summary>Le mois qui contient ce jour.</summary>
    public static PeriodeAtelier Mois(IServiceDateHeure horloge, DateTime jour)
    {
        var debut = new DateTime(jour.Year, jour.Month, 1);
        return Jours(horloge, debut, debut.AddMonths(1).AddDays(-1));
    }

    public bool Contient(DateTime utc) => utc >= DebutUtc && utc < FinUtc;

    public string Libelle => PremierJour == DernierJour
        ? $"Le {Formatage.Date(PremierJour)}"
        : $"Du {Formatage.Date(PremierJour)} au {Formatage.Date(DernierJour)}";
}
