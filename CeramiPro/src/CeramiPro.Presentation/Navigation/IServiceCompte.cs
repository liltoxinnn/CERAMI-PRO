namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Ce que la fenêtre principale demande au système d'exploitation à propos de
/// la session : changer le mot de passe, ou fermer la session.
///
/// Ces deux gestes ouvrent ou ferment des fenêtres : ils appartiennent à
/// l'application Windows, la vue-modèle n'en connaît que le contrat.
/// </summary>
public interface IServiceCompte
{
    /// <summary>Ouvre la fenêtre de changement de mot de passe.</summary>
    void ChangerMotDePasse();

    /// <summary>
    /// Ferme la session et redemande les identifiants. Le logiciel se ferme
    /// si personne ne se reconnecte.
    /// </summary>
    void Deconnecter();
}
