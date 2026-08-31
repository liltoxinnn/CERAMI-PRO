using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using CeramiPro.Application.Common;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Construit les contrôles de saisie à partir des champs décrits par une
/// vue-modèle.
///
/// Le même constructeur sert à la fenêtre de saisie et aux écrans de
/// document : un champ se présente donc partout de la même façon, et
/// ajouter un formulaire ne demande aucun XAML supplémentaire.
/// </summary>
public static class ConstructeurChamps
{
    /// <summary>
    /// Remplit un panneau avec l'étiquette, le contrôle et l'aide de chaque
    /// champ, tous liés à l'objet de requête.
    /// </summary>
    public static void Remplir(Panel panneau, IReadOnlyList<ChampFormulaire> champs, object requete)
    {
        panneau.Children.Clear();

        foreach (var champ in champs)
        {
            panneau.Children.Add(Etiquette(champ, panneau));
            panneau.Children.Add(Controle(champ, requete, panneau));

            if (!string.IsNullOrWhiteSpace(champ.Aide))
            {
                panneau.Children.Add(new TextBlock
                {
                    Text = champ.Aide,
                    FontSize = 11.5,
                    Foreground = (System.Windows.Media.Brush)panneau.FindResource("TexteSecondaire"),
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }
    }

    private static TextBlock Etiquette(ChampFormulaire champ, FrameworkElement racine) => new()
    {
        Text = champ.Obligatoire ? champ.Libelle + " *" : champ.Libelle,
        Style = (Style)racine.FindResource("EtiquetteChamp"),
        Margin = new Thickness(0, 12, 0, 4)
    };

    private static FrameworkElement Controle(
        ChampFormulaire champ, object requete, FrameworkElement racine)
    {
        var propriete = requete.GetType().GetProperty(champ.Propriete);

        var liaison = new Binding(champ.Propriete)
        {
            Source = requete,
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
            ConverterCulture = ParametresAtelier.Culture
        };

        switch (champ.Type)
        {
            case TypeChamp.Case:
                var caseACocher = new CheckBox { VerticalAlignment = VerticalAlignment.Center };
                caseACocher.SetBinding(ToggleButton.IsCheckedProperty, liaison);
                return caseACocher;

            case TypeChamp.Liste:
                // Les options portent un entier ; quand la propriété visée est
                // une énumération, il faut convertir, sans quoi la liaison
                // échouerait en silence et le champ resterait vide.
                if (TypeEnumeration(propriete?.PropertyType) is { } enumeration)
                {
                    liaison.Converter = new EntierEnEnumeration(enumeration);
                }

                var liste = new ComboBox
                {
                    ItemsSource = champ.Options,
                    DisplayMemberPath = nameof(OptionChamp.Libelle),
                    SelectedValuePath = nameof(OptionChamp.Valeur),
                    MinHeight = 38,
                    Padding = new Thickness(8, 6, 8, 6),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                liste.SetBinding(Selector.SelectedValueProperty, liaison);
                return liste;

            case TypeChamp.Date:
                var date = new DatePicker
                {
                    MinHeight = 38,
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                date.SetBinding(DatePicker.SelectedDateProperty, liaison);
                return date;

            case TypeChamp.TexteLong:
                var texteLong = new TextBox
                {
                    Style = (Style)racine.FindResource("Saisie"),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 72,
                    VerticalContentAlignment = VerticalAlignment.Top
                };
                texteLong.SetBinding(TextBox.TextProperty, liaison);
                return texteLong;

            default:
                var texte = new TextBox { Style = (Style)racine.FindResource("Saisie") };

                if (champ.Type is TypeChamp.Nombre or TypeChamp.Montant)
                {
                    texte.HorizontalContentAlignment = HorizontalAlignment.Right;
                }

                texte.SetBinding(TextBox.TextProperty, liaison);
                return texte;
        }
    }

    /// <summary>Type d'énumération visé, en tenant compte des propriétés facultatives.</summary>
    private static Type? TypeEnumeration(Type? type)
    {
        if (type is null)
        {
            return null;
        }

        var reel = Nullable.GetUnderlyingType(type) ?? type;

        return reel.IsEnum ? reel : null;
    }

    /// <summary>
    /// Traduit l'entier d'une option en valeur d'énumération, et l'inverse.
    /// Sans cela, choisir « Urgente » dans une liste de priorités ne serait
    /// jamais enregistré.
    /// </summary>
    private sealed class EntierEnEnumeration : IValueConverter
    {
        private readonly Type _enumeration;

        public EntierEnEnumeration(Type enumeration) => _enumeration = enumeration;

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is null ? null : System.Convert.ToInt32(value, CultureInfo.InvariantCulture);

        public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is null ? null : Enum.ToObject(_enumeration, System.Convert.ToInt32(value, culture));
    }
}
