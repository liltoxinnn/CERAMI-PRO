using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Écran de liste employé par tous les modules.
///
/// Les colonnes du tableau sont construites à partir de celles déclarées par
/// la vue-modèle : c'est ce qui permet à un seul fichier XAML de servir les
/// écrans de liste, avec la même ergonomie partout.
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
                // La colonne met elle-même sa valeur en forme : un montant
                // s'écrit ainsi à l'identique à l'écran, dans le tableur
                // exporté et sur le document imprimé.
                Binding = new Binding(colonne.Propriete)
                {
                    Converter = new ValeurDeColonne(colonne)
                },
                Width = double.IsNaN(colonne.Largeur)
                    ? new DataGridLength(1, DataGridLengthUnitType.Auto)
                    : new DataGridLength(colonne.Largeur),
                ElementStyle = style
            });
        }
    }

    /// <summary>
    /// Un double-clic ouvre la fiche : c'est le geste attendu dans un
    /// tableau, et il évite d'aller chercher le bouton « Modifier ».
    /// </summary>
    private void OuvrirLaFiche(object expediteur, MouseButtonEventArgs args)
    {
        if (DataContext is not { } contexte)
        {
            return;
        }

        var type = contexte.GetType();

        if (type.GetProperty("PeutModifier")?.GetValue(contexte) is not true)
        {
            return;
        }

        if (type.GetProperty("ModifierCommand")?.GetValue(contexte) is ICommand commande
            && commande.CanExecute(null))
        {
            commande.Execute(null);
        }
    }

    private static TextAlignment Alignement(ColonneListe colonne) => colonne.Alignement switch
    {
        ColonneAlignement.Droite => TextAlignment.Right,
        ColonneAlignement.Centre => TextAlignment.Center,
        _ => TextAlignment.Left
    };

    /// <summary>Applique à une cellule la mise en forme déclarée par sa colonne.</summary>
    private sealed class ValeurDeColonne : IValueConverter
    {
        private readonly ColonneListe _colonne;

        public ValeurDeColonne(ColonneListe colonne) => _colonne = colonne;

        public object Convert(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture) => _colonne.Formater(value);

        public object ConvertBack(object? value, Type targetType, object? parameter,
            System.Globalization.CultureInfo culture) => throw new NotSupportedException();
    }
}
