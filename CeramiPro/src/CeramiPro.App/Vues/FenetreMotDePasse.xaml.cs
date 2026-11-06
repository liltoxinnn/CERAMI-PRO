using System.ComponentModel;
using System.Windows;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Fenêtre de changement de mot de passe.
///
/// Les mots de passe transitent par des <see cref="System.Windows.Controls.PasswordBox"/>,
/// qui ne se lient pas : ils sont lus au moment de valider, puis effacés.
/// C'est ce qui évite qu'un mot de passe reste en mémoire dans une propriété
/// liée pendant toute la durée de la session.
/// </summary>
public partial class FenetreMotDePasse : Window
{
    public FenetreMotDePasse()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Suivre();
    }

    private void Suivre()
    {
        if (DataContext is not ChangementMotDePasseVueModele vueModele)
        {
            return;
        }

        vueModele.PropertyChanged += SurChangement;
        Actuel.Focus();
    }

    private void SurChangement(object? source, PropertyChangedEventArgs args)
    {
        if (args.PropertyName != nameof(ChangementMotDePasseVueModele.Change)
            || DataContext is not ChangementMotDePasseVueModele { Change: true })
        {
            return;
        }

        DialogResult = true;
    }

    private async void Valider(object expediteur, RoutedEventArgs args)
    {
        if (DataContext is not ChangementMotDePasseVueModele vueModele)
        {
            return;
        }

        vueModele.MotDePasseActuel = Actuel.Password;
        vueModele.NouveauMotDePasse = Nouveau.Password;
        vueModele.Confirmation = Confirmer.Password;

        await vueModele.ValiderCommand.ExecuteAsync(null);

        Actuel.Clear();
        Nouveau.Clear();
        Confirmer.Clear();
    }
}
