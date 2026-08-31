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
        IServiceProvider services)
    {
        Formulaires = formulaires;
        Dialogue = dialogue;
        Fichiers = fichiers;
        Exports = exports;
        Services = services;
    }

    public IServiceFormulaire Formulaires { get; }

    public IServiceDialogue Dialogue { get; }

    public IServiceFichier Fichiers { get; }

    public IExportService Exports { get; }

    /// <summary>Sert à construire la vue-modèle du formulaire de l'écran.</summary>
    public IServiceProvider Services { get; }
}
