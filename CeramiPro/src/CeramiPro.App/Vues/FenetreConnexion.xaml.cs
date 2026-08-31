using System.Windows;
using System.Windows.Input;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Écran de connexion.
///
/// Le mot de passe transite par le contrôle dédié de Windows, qui ne l'expose
/// pas sous forme de texte liable : il est donc recopié à la main vers la
/// vue-modèle, seule entorse acceptable au principe « aucun code dans la vue ».
/// </summary>
public partial class FenetreConnexion : Window
{
    public FenetreConnexion()
    {
        InitializeComponent();
        Loaded += (_, _) => ChampNomUtilisateur.Focus();
    }

    private ConnexionVueModele? VueModele => DataContext as ConnexionVueModele;

    private void ChampMotDePasse_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (VueModele is { } vue)
        {
            vue.MotDePasse = ChampMotDePasse.Password;
        }
    }

    private void ChampMotDePasse_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && VueModele?.ConnecterCommand.CanExecute(null) == true)
        {
            VueModele.ConnecterCommand.Execute(null);
        }
    }
}
