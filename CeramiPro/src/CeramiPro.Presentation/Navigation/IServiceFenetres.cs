using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Ouvre une fenêtre de consultation par-dessus l'écran courant : la fiche
/// d'un client, les photos d'un produit. La fenêtre charge elle-même ses
/// données, et l'appel rend la main à sa fermeture.
/// </summary>
public interface IServiceFenetres
{
    void Afficher(VueModeleBase vueModele);
}
