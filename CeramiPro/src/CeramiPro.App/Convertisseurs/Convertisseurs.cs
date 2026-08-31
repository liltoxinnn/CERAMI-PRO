using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CeramiPro.App.Convertisseurs;

/// <summary>Affiche un élément quand la valeur est vraie, le masque sinon.</summary>
public class BooleenEnVisibilite : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is Visibility.Visible;
}

/// <summary>Masque un élément quand la valeur est vraie, l'affiche sinon.</summary>
public class BooleenEnVisibiliteInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not Visibility.Visible;
}

/// <summary>
/// Couleur d'état : vert quand tout va bien, rouge sinon. La couleur ne porte
/// jamais l'information seule — elle accompagne toujours un texte.
/// </summary>
public class BooleenEnPastille : IValueConverter
{
    private static readonly SolidColorBrush Vert = new(Color.FromRgb(0x1F, 0x7A, 0x54));
    private static readonly SolidColorBrush Rouge = new(Color.FromRgb(0xB3, 0x26, 0x1E));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? Vert : Rouge;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Traduit le sens de lecture métier en sens WPF. En arabe, la fenêtre
/// entière s'inverse : le menu passe à droite, les colonnes et les champs
/// suivent, sans qu'aucun écran ait à s'en occuper.
/// </summary>
public class SensEnFlowDirection : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is CeramiPro.Application.Localisation.SensEcriture.DroiteAGauche
            ? FlowDirection.RightToLeft
            : FlowDirection.LeftToRight;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is FlowDirection.RightToLeft
            ? CeramiPro.Application.Localisation.SensEcriture.DroiteAGauche
            : CeramiPro.Application.Localisation.SensEcriture.GaucheADroite;
}

/// <summary>Nom d'une langue écrit dans sa propre langue.</summary>
public class LangueEnNom : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is CeramiPro.Application.Localisation.Langue langue
            ? CeramiPro.Application.Localisation.LangueInfo.NomNatif(langue)
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Inverse un booléen : sert à désactiver un bouton pendant un chargement.</summary>
public class BooleenInverse : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is not true;
}

/// <summary>
/// Affiche un bloc seulement lorsqu'il porte un texte. Un encadré d'erreur
/// vide ne doit pas occuper d'espace à l'écran.
/// </summary>
public class TexteEnVisibilite : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Couleur d'une alerte selon sa gravité. La couleur ne porte jamais
/// l'information seule : le libellé de la gravité est toujours affiché à
/// côté, afin que l'écran reste lisible par une personne daltonienne.
/// </summary>
public class GraviteEnCouleur : IValueConverter
{
    private static readonly SolidColorBrush Critique = new(Color.FromRgb(0xB3, 0x26, 0x1E));
    private static readonly SolidColorBrush Avertissement = new(Color.FromRgb(0x8A, 0x5B, 0x00));
    private static readonly SolidColorBrush Information = new(Color.FromRgb(0x1C, 0x5D, 0x99));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value switch
        {
            CeramiPro.Domain.Enums.NotificationSeverity.Critique => Critique,
            CeramiPro.Domain.Enums.NotificationSeverity.Avertissement => Avertissement,
            _ => Information
        };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Écrit un booléen en toutes lettres. Une case cochée est ambiguë dans un
/// tableau imprimé ; « Oui » et « Non » ne le sont jamais.
/// </summary>
public class BooleenEnOuiNon : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is true ? "Oui" : "Non";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is "Oui";
}
