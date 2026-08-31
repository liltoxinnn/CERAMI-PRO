using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.Presentation.Navigation;

/// <summary>Passage d'un écran à l'autre dans la fenêtre principale.</summary>
public interface IServiceNavigation
{
    /// <summary>Écran actuellement affiché.</summary>
    VueModeleBase? VueCourante { get; }

    event Action<VueModeleBase>? VueChangee;

    void Naviguer<TVueModele>() where TVueModele : VueModeleBase;

    void Naviguer(Type typeVueModele);
}
