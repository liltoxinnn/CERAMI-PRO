using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Écran de liste employé par tous les modules.
///
/// Les colonnes du tableau sont construites à partir de celles déclarées par
/// la vue-modèle : c'est ce qui permet à un seul fichier XAML de servir les
/// seize écrans de liste, avec la même ergonomie partout.
/// </summary>
public partial class VueListe : UserControl
{
    public VueListe()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => ConstruireColonnes();
    }

    private void ConstruireColonnes()
    {
        Tableau.Columns.Clear();

        if (DataContext is not { } contexte)
        {
            return;
        }

        var colonnes = contexte.GetType().GetProperty(nameof(ListeVueModele<object>.Colonnes))
            ?.GetValue(contexte) as IReadOnlyList<ColonneListe>;

        if (colonnes is null)
        {
            return;
        }

        foreach (var colonne in colonnes)
        {
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, Alignement(colonne)));
            style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(10, 0, 10, 0)));
            style.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));

            Tableau.Columns.Add(new DataGridTextColumn
            {
                Header = colonne.EnTete,
                Binding = new Binding(colonne.Propriete) { StringFormat = colonne.Format },
                Width = double.IsNaN(colonne.Largeur)
                    ? new DataGridLength(1, DataGridLengthUnitType.Auto)
                    : new DataGridLength(colonne.Largeur),
                ElementStyle = style
            });
        }
    }

    private static TextAlignment Alignement(ColonneListe colonne) => colonne.Alignement switch
    {
        ColonneAlignement.Droite => TextAlignment.Right,
        ColonneAlignement.Centre => TextAlignment.Center,
        _ => TextAlignment.Left
    };
}
