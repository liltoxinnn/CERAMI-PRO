using System.Windows;

namespace CeramiPro.App.Vues;

/// <summary>Galerie des photos d'un produit ou d'une commande.</summary>
public partial class FenetrePhotos : Window
{
    public FenetrePhotos() => InitializeComponent();

    private void Fermer(object sender, RoutedEventArgs e) => Close();
}
