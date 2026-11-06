using CeramiPro.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Navigation entre les écrans.
///
/// Chaque écran vit dans sa propre portée d'injection, et donc avec son
/// propre contexte de base de données. Sans cela, tous les écrans se
/// partageraient le même : passer d'un écran à l'autre pendant qu'un
/// chargement est en cours ferait échouer les deux, le contexte n'acceptant
/// qu'une opération à la fois.
///
/// La portée précédente est fermée après le changement : l'écran quitté ne
/// s'affiche plus, ses données n'ont plus à vivre.
/// </summary>
public class ServiceNavigation : IServiceNavigation, IDisposable
{
    private readonly IServiceProvider _services;
    private IServiceScope? _portee;

    public ServiceNavigation(IServiceProvider services) => _services = services;

    public VueModeleBase? VueCourante { get; private set; }

    /// <summary>
    /// Chargement de l'écran affiché. L'attendre n'est utile qu'aux tests :
    /// l'interface, elle, s'affiche sans attendre ses données.
    /// </summary>
    public Task ChargementCourant { get; private set; } = Task.CompletedTask;

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

        var ancienne = _portee;
        var nouvelle = _services.CreateScope();

        VueModeleBase vue;

        try
        {
            vue = (VueModeleBase)nouvelle.ServiceProvider.GetRequiredService(typeVueModele);
        }
        catch
        {
            // L'écran n'a pas pu être construit : la portée neuve n'a plus
            // de raison d'être, et l'écran précédent reste affiché.
            nouvelle.Dispose();
            throw;
        }

        _portee = nouvelle;
        VueCourante = vue;
        VueChangee?.Invoke(vue);

        // Le chargement des données ne bloque pas l'affichage de l'écran.
        ChargementCourant = vue.ChargerAsync();

        ancienne?.Dispose();
    }

    public void Dispose()
    {
        _portee?.Dispose();
        _portee = null;

        GC.SuppressFinalize(this);
    }
}
