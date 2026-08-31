using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Base de toutes les vues-modèles : titre affiché dans l'en-tête, indicateur
/// de chargement, et message d'erreur éventuel.
/// </summary>
public abstract partial class VueModeleBase : ObservableObject
{
    [ObservableProperty]
    private bool _chargementEnCours;

    [ObservableProperty]
    private string? _messageErreur;

    /// <summary>Titre affiché en haut de l'écran.</summary>
    public abstract string Titre { get; }

    /// <summary>Phrase explicative sous le titre.</summary>
    public virtual string? Introduction => null;

    /// <summary>
    /// Chargement des données de l'écran. Appelé à chaque affichage, jamais
    /// depuis le constructeur : une vue-modèle doit pouvoir être créée sans
    /// toucher à la base de données.
    /// </summary>
    public virtual Task ChargerAsync() => Task.CompletedTask;

    /// <summary>
    /// Exécute une opération en affichant l'indicateur de chargement et en
    /// transformant toute erreur en message lisible.
    /// </summary>
    protected async Task ExecuterAsync(Func<Task> operation, Func<Exception, string>? messageErreur = null)
    {
        ChargementEnCours = true;
        MessageErreur = null;

        try
        {
            await operation();
        }
        catch (Application.Common.RegleMetierException erreur)
        {
            MessageErreur = erreur.Message;
        }
        catch (Exception erreur)
        {
            MessageErreur = messageErreur?.Invoke(erreur)
                ?? "L'opération n'a pas pu être effectuée. Veuillez réessayer.";
        }
        finally
        {
            ChargementEnCours = false;
        }
    }
}
