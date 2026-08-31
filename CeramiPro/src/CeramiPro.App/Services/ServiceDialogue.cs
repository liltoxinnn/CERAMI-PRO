using System.Windows;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.App.Services;

/// <summary>
/// Messages affichés à l'utilisateur, en français, avec des titres explicites.
/// Les vues-modèles ne connaissent que l'interface : elles restent testables.
/// </summary>
public class ServiceDialogue : IServiceDialogue
{
    public void Information(string message, string titre = "Information")
        => Afficher(message, titre, MessageBoxImage.Information);

    public void Succes(string message, string titre = "Opération réussie")
        => Afficher(message, titre, MessageBoxImage.Information);

    public void Avertissement(string message, string titre = "Attention")
        => Afficher(message, titre, MessageBoxImage.Warning);

    public void Erreur(string message, string titre = "Erreur")
        => Afficher(message, titre, MessageBoxImage.Error);

    public bool Confirmer(string message, string titre = "Confirmation")
        => MessageBox.Show(
               message, $"CeramiPro — {titre}",
               MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No)
           == MessageBoxResult.Yes;

    private static void Afficher(string message, string titre, MessageBoxImage icone)
        => MessageBox.Show(message, $"CeramiPro — {titre}", MessageBoxButton.OK, icone);
}
