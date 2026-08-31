using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.Presentation.Navigation;

/// <summary>Passage d'un écran à l'autre dans la fenêtre principale.</summary>
public interface IServiceNavigation
{
    /// <summary>Écran actuellement affiché.</summary>
    VueModeleBase? VueCourante { get; }

    /// <summary>
    /// Chargement de l'écran affiché. L'interface n'a pas à l'attendre — elle
    /// s'affiche d'abord, ses données ensuite — mais les tests en ont besoin
    /// pour savoir quand regarder le résultat.
    /// </summary>
    Task ChargementCourant { get; }

    event Action<VueModeleBase>? VueChangee;

    void Naviguer<TVueModele>() where TVueModele : VueModeleBase;

    void Naviguer(Type typeVueModele);
}
