namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Ouvre une fenêtre de saisie et attend sa fermeture. Passer par une
/// interface permet de tester les écrans de liste sans ouvrir de fenêtre.
/// </summary>
public interface IServiceFormulaire
{
    /// <summary>
    /// Affiche le formulaire correspondant à la vue-modèle fournie.
    /// Renvoie vrai si la saisie a été enregistrée.
    /// </summary>
    bool Afficher(object vueModeleFormulaire);
}
