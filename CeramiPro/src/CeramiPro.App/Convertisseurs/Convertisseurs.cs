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
