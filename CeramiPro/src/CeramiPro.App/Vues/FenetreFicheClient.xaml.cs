using System.Windows;

namespace CeramiPro.App.Vues;

/// <summary>Fiche d'un client, avec tout son historique.</summary>
public partial class FenetreFicheClient : Window
{
    public FenetreFicheClient() => InitializeComponent();

    private void Fermer(object sender, RoutedEventArgs e) => Close();
}
