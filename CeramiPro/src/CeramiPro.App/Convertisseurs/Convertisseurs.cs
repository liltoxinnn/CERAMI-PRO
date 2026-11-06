using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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

/// <summary>
/// Choisit l'allure d'un bouton d'action : une action qui détruit ou annule
/// se distingue des autres, pour ne pas être cliquée par mégarde.
/// </summary>
public class ActionEnStyle : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var cle = value is true ? "BoutonDanger" : "BoutonSecondaire";

        return System.Windows.Application.Current?.TryFindResource(cle);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>
/// Charge une photo depuis son fichier, en miniature si une largeur est
/// donnée en paramètre.
///
/// L'image est lue entièrement puis le fichier est relâché : sans cela,
/// Windows le garderait verrouillé et la photo ne pourrait pas être retirée
/// pendant qu'elle est affichée.
/// </summary>
public class CheminEnImage : IValueConverter
{
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string chemin || string.IsNullOrWhiteSpace(chemin) || !File.Exists(chemin))
        {
            return null;
        }

        try
        {
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
            image.UriSource = new Uri(chemin, UriKind.Absolute);

            if (parameter is string largeur && int.TryParse(largeur, out var pixels))
            {
                image.DecodePixelWidth = pixels;
            }

            image.EndInit();
            image.Freeze();
            return image;
        }
        catch (Exception)
        {
            // Un fichier abîmé ne doit pas empêcher d'afficher les autres photos.
            return null;
        }
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Montant en dinars, écrit comme partout ailleurs : « 12 500,00 DA ».</summary>
public class MontantEnTexte : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is decimal montant ? CeramiPro.Application.Common.Formatage.Montant(montant) : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

/// <summary>Date « 31/08/2026 », ou date et heure si le paramètre vaut « heure ».</summary>
public class DateEnTexte : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is DateTime date
            ? (parameter as string == "heure"
                ? CeramiPro.Application.Common.Formatage.DateHeure(date)
                : CeramiPro.Application.Common.Formatage.Date(date))
            : string.Empty;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
