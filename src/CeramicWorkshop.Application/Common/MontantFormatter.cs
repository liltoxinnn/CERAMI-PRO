using System.Globalization;

namespace CeramicWorkshop.Application.Common;

/// <summary>
/// Mise en forme des montants et des dates au format algérien : « 45 000,00 DA », « 15/09/2026 ».
/// La devise et le nombre de décimales proviennent des paramètres de l'atelier,
/// ce qui permettra d'utiliser une autre monnaie sans modifier le code.
/// </summary>
public static class MontantFormatter
{
    /// <summary>Espace insécable étroit utilisé comme séparateur de milliers.</summary>
    public const string SeparateurMilliers = " ";

    public static readonly CultureInfo CultureAtelier = CreerCulture();

    private static CultureInfo CreerCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();
        culture.NumberFormat.NumberGroupSeparator = SeparateurMilliers;
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
        culture.DateTimeFormat.ShortTimePattern = "HH:mm";
        return culture;
    }

    /// <summary>Formate un montant, par exemple « 45 000,00 DA ».</summary>
    public static string Formater(decimal montant, string symbole = "DA", int decimales = 2)
        => string.Concat(montant.ToString("N" + decimales, CultureAtelier), " ", symbole);

    /// <summary>Formate une quantité en supprimant les décimales inutiles.</summary>
    public static string FormaterQuantite(decimal quantite, string? unite = null)
    {
        var arrondi = Math.Round(quantite, 3);
        var texte = arrondi == Math.Truncate(arrondi)
            ? arrondi.ToString("N0", CultureAtelier)
            : arrondi.ToString("0.###", CultureAtelier);
        return string.IsNullOrWhiteSpace(unite) ? texte : $"{texte} {unite}";
    }

    /// <summary>Formate une date au format jour/mois/année.</summary>
    public static string FormaterDate(DateTime date) => date.ToString("dd/MM/yyyy", CultureAtelier);

    /// <summary>Formate une date avec l'heure.</summary>
    public static string FormaterDateHeure(DateTime date) => date.ToString("dd/MM/yyyy HH:mm", CultureAtelier);
}
