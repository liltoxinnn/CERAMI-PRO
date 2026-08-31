namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Demande à l'utilisateur où enregistrer un fichier. Passer par une
/// interface permet de tester les écrans sans ouvrir de boîte de dialogue.
/// </summary>
public interface IServiceFichier
{
    /// <summary>
    /// Renvoie le chemin choisi, ou <c>null</c> si l'utilisateur renonce.
    /// </summary>
    string? DemanderOuEnregistrer(string nomPropose, string filtre);

    /// <summary>Ouvre un fichier avec le programme associé de Windows.</summary>
    void Ouvrir(string chemin);
}
