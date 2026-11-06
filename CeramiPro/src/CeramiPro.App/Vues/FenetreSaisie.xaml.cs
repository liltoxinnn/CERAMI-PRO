using System.Windows;

namespace CeramiPro.App.Vues;

/// <summary>Fenêtre qui demande un court texte à l'utilisateur.</summary>
public partial class FenetreSaisie : Window
{
    private readonly bool _obligatoire;

    public FenetreSaisie(string question, string titre, string valeurInitiale, bool obligatoire)
    {
        InitializeComponent();

        Title = titre;
        Question.Text = question;
        Reponse.Text = valeurInitiale;
        _obligatoire = obligatoire;

        Loaded += (_, _) =>
        {
            Reponse.Focus();
            Reponse.SelectAll();
        };
    }

    /// <summary>Texte saisi, sans espaces autour.</summary>
    public string Valeur => Reponse.Text.Trim();

    private void Valider(object sender, RoutedEventArgs e)
    {
        if (_obligatoire && string.IsNullOrWhiteSpace(Reponse.Text))
        {
            Erreur.Visibility = Visibility.Visible;
            Reponse.Focus();
            return;
        }

        DialogResult = true;
    }
}
