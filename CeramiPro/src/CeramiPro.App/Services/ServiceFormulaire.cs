using System.Windows;
using CeramiPro.App.Vues;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.App.Services;

/// <summary>Affiche la fenêtre de saisie générique au-dessus de la fenêtre principale.</summary>
public class ServiceFormulaire : IServiceFormulaire
{
    public bool Afficher(object vueModeleFormulaire)
    {
        var fenetre = new FenetreFormulaire
        {
            Owner = System.Windows.Application.Current.MainWindow,
            DataContext = vueModeleFormulaire
        };

        return fenetre.ShowDialog() == true;
    }
}
