using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CeramiPro.Presentation.ViewModels.Ecrans;

namespace CeramiPro.App.Vues;

/// <summary>
/// Écran des rapports.
///
/// Chaque rapport a ses propres colonnes : elles sont donc reconstruites à
/// chaque affichage, à partir des en-têtes renvoyés par le service. Les
/// lignes étant de simples listes de textes, chaque colonne se lie à son
/// rang, ce qui évite de créer un type par rapport.
/// </summary>
public partial class RapportsVue : UserControl
{
    public RapportsVue()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Suivre();
    }

    private void Suivre()
    {
        if (DataContext is not RapportsVueModele vueModele)
        {
            return;
        }

        ConstruireColonnes(vueModele);
        vueModele.Colonnes.CollectionChanged += (_, _) => ConstruireColonnes(vueModele);
    }

    private void ConstruireColonnes(RapportsVueModele vueModele)
    {
        Tableau.Columns.Clear();

        for (var rang = 0; rang < vueModele.Colonnes.Count; rang++)
        {
            // La première colonne porte le libellé, les suivantes des nombres :
            // les aligner à droite rend les montants comparables d'un coup d'œil.
            var style = new Style(typeof(TextBlock));
            style.Setters.Add(new Setter(
                TextBlock.TextAlignmentProperty,
                rang == 0 ? TextAlignment.Left : TextAlignment.Right));
            style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(10, 0, 10, 0)));
            style.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));

            Tableau.Columns.Add(new DataGridTextColumn
            {
                Header = vueModele.Colonnes[rang],
                Binding = new Binding($"[{rang}]"),
                Width = rang == 0
                    ? new DataGridLength(2, DataGridLengthUnitType.Star)
                    : new DataGridLength(1, DataGridLengthUnitType.Star),
                ElementStyle = style
            });
        }
    }
}
