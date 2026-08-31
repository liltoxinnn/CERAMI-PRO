using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Écran de saisie d'un document ligne par ligne.
///
/// Les colonnes du tableau et les champs d'en-tête sont construits ici : la
/// caisse, l'achat et l'enfournement partagent ainsi le même écran, chacun
/// n'ayant à décrire que ce qui lui est propre.
/// </summary>
public partial class VueDocument : UserControl
{
    private INotifyPropertyChanged? _observee;

    public VueDocument()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Suivre();
    }

    private void Suivre()
    {
        if (_observee is not null)
        {
            _observee.PropertyChanged -= SurChangement;
            _observee = null;
        }

        ConstruireColonnes();
        ConstruireChamps();

        if (DataContext is INotifyPropertyChanged observable)
        {
            _observee = observable;
            observable.PropertyChanged += SurChangement;
        }

        // Le curseur attend dans la zone de scan : à la caisse, on enchaîne
        // les produits sans jamais toucher la souris.
        if (DataContext?.GetType().GetProperty("AccepteScan")?.GetValue(DataContext) is true)
        {
            ZoneScan.Focus();
        }
    }

    private void SurChangement(object? source, PropertyChangedEventArgs args)
    {
        // Les listes déroulantes de l'en-tête arrivent après le premier
        // affichage : les champs doivent alors être reconstruits.
        if (args.PropertyName is "Champs" or "Requete")
        {
            ConstruireChamps();
        }
    }

    /// <summary>
    /// Colonnes du tableau des lignes. Les colonnes de prix disparaissent
    /// quand le document n'en comporte pas — un enfournement, par exemple.
    /// </summary>
    private void ConstruireColonnes()
    {
        Tableau.Columns.Clear();

        if (DataContext is not { } contexte)
        {
            return;
        }

        var afficherPrix = contexte.GetType().GetProperty("AfficherPrix")?.GetValue(contexte) is not false;

        Tableau.Columns.Add(Colonne("Article", nameof(LigneDocument.Nom), etoiles: 3));
        Tableau.Columns.Add(Colonne("Quantité", nameof(LigneDocument.QuantiteAffichee), aDroite: true));

        if (afficherPrix)
        {
            Tableau.Columns.Add(Colonne(
                "Prix unitaire", nameof(LigneDocument.PrixUnitaireAffiche), aDroite: true));
            Tableau.Columns.Add(Colonne(
                "Remise", nameof(LigneDocument.RemiseAffichee), aDroite: true));
            Tableau.Columns.Add(Colonne(
                "Total", nameof(LigneDocument.TotalAffiche), aDroite: true));
        }
    }

    private static DataGridTextColumn Colonne(
        string entete, string propriete, bool aDroite = false, int etoiles = 1)
    {
        var style = new Style(typeof(TextBlock));
        style.Setters.Add(new Setter(
            TextBlock.TextAlignmentProperty, aDroite ? TextAlignment.Right : TextAlignment.Left));
        style.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(10, 0, 10, 0)));
        style.Setters.Add(new Setter(VerticalAlignmentProperty, VerticalAlignment.Center));

        return new DataGridTextColumn
        {
            Header = entete,
            Binding = new Binding(propriete),
            Width = new DataGridLength(etoiles, DataGridLengthUnitType.Star),
            ElementStyle = style
        };
    }

    private void ConstruireChamps()
    {
        ZoneChamps.Children.Clear();

        if (DataContext is not { } contexte)
        {
            return;
        }

        var type = contexte.GetType();

        if (type.GetProperty("Champs")?.GetValue(contexte) is not IReadOnlyList<ChampFormulaire> champs
            || type.GetProperty("Requete")?.GetValue(contexte) is not { } requete)
        {
            return;
        }

        ConstructeurChamps.Remplir(ZoneChamps, champs, requete);
    }
}
