using CeramiPro.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Navigation entre les écrans. Chaque vue-modèle est construite par
/// l'injection de dépendances, puis on lui demande de charger ses données.
/// </summary>
public class ServiceNavigation : IServiceNavigation
{
    private readonly IServiceProvider _services;

    public ServiceNavigation(IServiceProvider services) => _services = services;

    public VueModeleBase? VueCourante { get; private set; }

    public event Action<VueModeleBase>? VueChangee;

    public void Naviguer<TVueModele>() where TVueModele : VueModeleBase
        => Naviguer(typeof(TVueModele));

    public void Naviguer(Type typeVueModele)
    {
        if (!typeof(VueModeleBase).IsAssignableFrom(typeVueModele))
        {
            throw new ArgumentException(
                $"« {typeVueModele.Name} » n'est pas un écran de l'application.", nameof(typeVueModele));
        }

        var vue = (VueModeleBase)_services.GetRequiredService(typeVueModele);

        VueCourante = vue;
        VueChangee?.Invoke(vue);

        // Le chargement des données ne bloque pas l'affichage de l'écran.
        _ = vue.ChargerAsync();
    }
}
