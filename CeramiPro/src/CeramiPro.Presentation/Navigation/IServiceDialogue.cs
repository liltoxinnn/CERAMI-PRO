namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Messages et confirmations affichés à l'utilisateur. Le passage par une
/// interface permet de tester les vues-modèles sans ouvrir de fenêtre.
/// </summary>
public interface IServiceDialogue
{
    void Information(string message, string titre = "Information");

    void Succes(string message, string titre = "Opération réussie");

    void Avertissement(string message, string titre = "Attention");

    void Erreur(string message, string titre = "Erreur");

    /// <summary>Demande une confirmation ; renvoie vrai si l'utilisateur accepte.</summary>
    bool Confirmer(string message, string titre = "Confirmation");
}
