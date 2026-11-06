using System.Windows;
using CeramiPro.App.Vues;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Services;

/// <summary>
/// Ouvre les fenêtres de consultation par-dessus l'écran courant. Chaque
/// vue-modèle a sa fenêtre ; une vue-modèle inconnue est une erreur de
/// programmation, signalée tout de suite plutôt qu'ignorée.
/// </summary>
public class ServiceFenetres : IServiceFenetres
{
    public void Afficher(VueModeleBase vueModele)
    {
        Window fenetre = vueModele switch
        {
            GaleriePhotosVueModele => new FenetrePhotos(),
            FicheClientVueModele => new FenetreFicheClient(),
            _ => throw new InvalidOperationException(
                $"Aucune fenêtre n'est prévue pour {vueModele.GetType().Name}.")
        };

        fenetre.DataContext = vueModele;
        fenetre.Owner = System.Windows.Application.Current?.MainWindow;

        // Les données se chargent une fois la fenêtre affichée : elle
        // s'ouvre aussitôt, et ses listes se remplissent ensuite.
        fenetre.Loaded += async (_, _) => await vueModele.ChargerAsync();

        fenetre.ShowDialog();
    }
}
