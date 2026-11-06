using CeramiPro.Presentation.ViewModels;
using CeramiPro.Application.Interfaces;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Les quelques services dont tout écran de liste a besoin : ouvrir une
/// fiche, demander une confirmation, enregistrer un export.
///
/// Les regrouper évite d'ajouter cinq paramètres au constructeur de chacun
/// des dix-huit écrans, et permet d'en ajouter un plus tard sans les
/// reprendre un par un.
/// </summary>
public class OutilsListe
{
    public OutilsListe(
        IServiceFormulaire formulaires,
        IServiceDialogue dialogue,
        IServiceFichier fichiers,
        IExportService exports,
        IServiceFenetres fenetres,
        IServiceProvider services)
    {
        Fenetres = fenetres;
        Formulaires = formulaires;
        Dialogue = dialogue;
        Fichiers = fichiers;
        Exports = exports;
        Services = services;
    }

    public IServiceFormulaire Formulaires { get; }

    /// <summary>Fenêtres de consultation : fiche client, photos.</summary>
    public IServiceFenetres Fenetres { get; }

    public IServiceDialogue Dialogue { get; }

    public IServiceFichier Fichiers { get; }

    public IExportService Exports { get; }

    /// <summary>Sert à construire la vue-modèle du formulaire de l'écran.</summary>
    public IServiceProvider Services { get; }

    /// <summary>
    /// Ouvre un formulaire, après avoir chargé ses listes déroulantes : il
    /// ne doit jamais s'afficher sur des choix vides. La préparation permet
    /// de le pré-remplir. Renvoie vrai si la saisie a été enregistrée.
    /// </summary>
    public async Task<bool> OuvrirFormulaireAsync(
        Type typeFormulaire, Func<IFormulaire, Task>? preparer = null)
    {
        if (Services.GetService(typeFormulaire) is not IFormulaire formulaire)
        {
            return false;
        }

        await formulaire.PreparerAsync();

        if (preparer is not null)
        {
            await preparer(formulaire);
        }

        return Formulaires.Afficher(formulaire);
    }

    /// <summary>Ouvre un formulaire pré-rempli par une action sur ce formulaire.</summary>
    public Task<bool> OuvrirFormulaireAsync<TFormulaire>(Func<TFormulaire, Task>? preparer = null)
        where TFormulaire : class, IFormulaire
        => OuvrirFormulaireAsync(typeof(TFormulaire),
            preparer is null ? null : formulaire => preparer((TFormulaire)formulaire));
}
