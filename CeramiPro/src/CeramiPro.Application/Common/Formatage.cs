namespace CeramiPro.Application.Common;

/// <summary>
/// Mise en forme des montants, quantités et dates telles qu'elles doivent
/// apparaître à l'écran et sur les documents imprimés.
///
/// Les espaces employées sont insécables et déclarées explicitement : sans
/// cela, un montant pourrait être coupé en fin de ligne entre le nombre et sa
/// devise, ce qui rend un document illisible.
/// </summary>
public static class Formatage
{
    /// <summary>Espace fine insécable (U+202F) : séparateur de milliers français.</summary>
    public const char EspaceMilliers = '\u202F';

    /// <summary>Espace insécable (U+00A0) : entre un nombre et son unité ou sa devise.</summary>
    public const char EspaceUnite = '\u00A0';

    /// <summary>Montant en dinars : <c>45 000,00 DA</c>.</summary>
    public static string Montant(decimal valeur)
        => valeur.ToString("N2", ParametresAtelier.Culture)
           + EspaceUnite
           + ParametresAtelier.SymboleDevise;

    /// <summary>Quantité sans décimale inutile : <c>1,5 kg</c>, <c>3 pièces</c>.</summary>
    public static string Quantite(decimal valeur, string? unite = null)
    {
        var nombre = valeur == decimal.Truncate(valeur)
            ? valeur.ToString("N0", ParametresAtelier.Culture)
            : valeur.ToString("0.###", ParametresAtelier.Culture);

        return string.IsNullOrWhiteSpace(unite) ? nombre : nombre + EspaceUnite + unite;
    }

    /// <summary>Date au format algérien : <c>31/08/2026</c>.</summary>
    public static string Date(DateTime valeur)
        => valeur.ToString("dd/MM/yyyy", ParametresAtelier.Culture);

    /// <summary>Date et heure : <c>31/08/2026 14:30</c>.</summary>
    public static string DateHeure(DateTime valeur)
        => valeur.ToString("dd/MM/yyyy HH:mm", ParametresAtelier.Culture);

    /// <summary>Pourcentage : <c>12,5 %</c>.</summary>
    public static string Pourcentage(decimal valeur)
        => valeur.ToString("0.##", ParametresAtelier.Culture) + EspaceUnite + "%";
}
