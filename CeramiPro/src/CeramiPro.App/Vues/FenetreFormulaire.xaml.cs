using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CeramiPro.Application.Common;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Fenêtre de saisie employée par tous les formulaires.
///
/// Les contrôles sont construits à partir des champs déclarés par la
/// vue-modèle, et liés directement à l'objet de requête : un seul fichier
/// XAML sert ainsi tous les modules, avec la même présentation.
/// </summary>
public partial class FenetreFormulaire : Window
{
    public FenetreFormulaire()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ConstruireChamps();
    }

    private void ConstruireChamps()
    {
        ZoneChamps.Children.Clear();

        if (DataContext is not { } vueModele)
        {
            return;
        }

        var type = vueModele.GetType();

        if (type.GetProperty("Champs")?.GetValue(vueModele) is not IReadOnlyList<ChampFormulaire> champs
            || type.GetProperty("Requete")?.GetValue(vueModele) is not { } requete)
        {
            return;
        }

        // La fenêtre se ferme d'elle-même quand l'enregistrement a réussi.
        if (vueModele is System.ComponentModel.INotifyPropertyChanged observable)
        {
            observable.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == "Enregistre"
                    && type.GetProperty("Enregistre")?.GetValue(vueModele) is true)
                {
                    DialogResult = true;
                }
            };
        }

        foreach (var champ in champs)
        {
            ZoneChamps.Children.Add(Etiquette(champ));
            ZoneChamps.Children.Add(Controle(champ, requete));

            if (!string.IsNullOrWhiteSpace(champ.Aide))
            {
                ZoneChamps.Children.Add(new TextBlock
                {
                    Text = champ.Aide,
                    FontSize = 11.5,
                    Foreground = (System.Windows.Media.Brush)FindResource("TexteSecondaire"),
                    Margin = new Thickness(0, 3, 0, 0),
                    TextWrapping = TextWrapping.Wrap
                });
            }
        }
    }

    private TextBlock Etiquette(ChampFormulaire champ) => new()
    {
        Text = champ.Obligatoire ? champ.Libelle + " *" : champ.Libelle,
        Style = (Style)FindResource("EtiquetteChamp"),
        Margin = new Thickness(0, 12, 0, 4)
    };

    private FrameworkElement Controle(ChampFormulaire champ, object requete)
    {
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
                caseACocher.SetBinding(ToggleButton_IsCheckedProperty, liaison);
                return caseACocher;

            case TypeChamp.Liste:
                var liste = new ComboBox
                {
                    ItemsSource = champ.Options,
                    DisplayMemberPath = nameof(OptionChamp.Libelle),
                    SelectedValuePath = nameof(OptionChamp.Valeur),
                    MinHeight = 38,
                    Padding = new Thickness(8, 6, 8, 6),
                    VerticalContentAlignment = VerticalAlignment.Center
                };
                liste.SetBinding(Selector_SelectedValueProperty, liaison);
                return liste;

            case TypeChamp.Date:
                var date = new DatePicker { MinHeight = 38, VerticalContentAlignment = VerticalAlignment.Center };
                date.SetBinding(DatePicker.SelectedDateProperty, liaison);
                return date;

            case TypeChamp.TexteLong:
                var texteLong = new TextBox
                {
                    Style = (Style)FindResource("Saisie"),
                    AcceptsReturn = true,
                    TextWrapping = TextWrapping.Wrap,
                    MinHeight = 72,
                    VerticalContentAlignment = VerticalAlignment.Top
                };
                texteLong.SetBinding(TextBox.TextProperty, liaison);
                return texteLong;

            default:
                var texte = new TextBox { Style = (Style)FindResource("Saisie") };

                if (champ.Type is TypeChamp.Nombre or TypeChamp.Montant)
                {
                    texte.HorizontalContentAlignment = HorizontalAlignment.Right;
                }

                texte.SetBinding(TextBox.TextProperty, liaison);
                return texte;
        }
    }

    private static readonly DependencyProperty ToggleButton_IsCheckedProperty =
        System.Windows.Controls.Primitives.ToggleButton.IsCheckedProperty;

    private static readonly DependencyProperty Selector_SelectedValueProperty =
        System.Windows.Controls.Primitives.Selector.SelectedValueProperty;
}
