using System.Globalization;

namespace CeramiPro.Application.Common;

/// <summary>
/// Réglages régionaux de l'atelier. Ils sont rassemblés ici pour qu'un
/// changement de pays ou de monnaie ne touche qu'un seul fichier.
/// </summary>
public static class ParametresAtelier
{
    public const string CodePays = "DZ";
    public const string CodeDevise = "DZD";
    public const string SymboleDevise = "DA";
    public const string FuseauHoraire = "Africa/Algiers";
    public const string NomBaseDeDonnees = "CeramiProDB";

    /// <summary>
    /// Culture d'affichage : français, séparateur de milliers par espace
    /// insécable fine et virgule décimale, dates au format 31/08/2026.
    /// </summary>
    public static CultureInfo Culture { get; } = ConstruireCulture();

    private static CultureInfo ConstruireCulture()
    {
        var culture = (CultureInfo)CultureInfo.GetCultureInfo("fr-FR").Clone();

        culture.NumberFormat.CurrencySymbol = SymboleDevise;
        culture.NumberFormat.CurrencyDecimalSeparator = ",";
        culture.NumberFormat.CurrencyGroupSeparator = "\u202F";
        culture.NumberFormat.NumberDecimalSeparator = ",";
        culture.NumberFormat.NumberGroupSeparator = "\u202F";
        culture.NumberFormat.CurrencyPositivePattern = 3;   // 45 000,00 DA
        culture.NumberFormat.CurrencyNegativePattern = 8;   // -45 000,00 DA

        culture.DateTimeFormat.ShortDatePattern = "dd/MM/yyyy";
        culture.DateTimeFormat.LongDatePattern = "dddd d MMMM yyyy";
        culture.DateTimeFormat.ShortTimePattern = "HH:mm";

        return CultureInfo.ReadOnly(culture);
    }
}
