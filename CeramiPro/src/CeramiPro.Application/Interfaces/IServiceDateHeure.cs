namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Horloge de l'atelier. Les dates sont stockées en UTC et affichées à
/// l'heure locale d'Alger. Passer par cette interface rend les règles
/// dépendantes du temps vérifiables par des tests.
/// </summary>
public interface IServiceDateHeure
{
    /// <summary>Instant présent, en temps universel — ce qui est stocké.</summary>
    DateTime MaintenantUtc { get; }

    /// <summary>Instant présent à l'heure de l'atelier — ce qui est affiché.</summary>
    DateTime MaintenantAtelier { get; }

    /// <summary>Date du jour à l'atelier, sans l'heure.</summary>
    DateTime Aujourdhui { get; }

    DateTime VersHeureAtelier(DateTime utc);

    DateTime VersUtc(DateTime heureAtelier);
}
