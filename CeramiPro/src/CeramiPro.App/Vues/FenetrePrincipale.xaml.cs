using System.Windows;

namespace CeramiPro.App.Vues;

/// <summary>
/// Fenêtre principale : menu latéral à gauche, en-tête et écran courant à
/// droite. Elle ne contient aucune logique métier — tout passe par sa
/// vue-modèle.
/// </summary>
public partial class FenetrePrincipale : Window
{
    public FenetrePrincipale() => InitializeComponent();
}
