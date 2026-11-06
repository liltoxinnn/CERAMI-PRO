namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Choix de fichiers : où enregistrer un document, quelles photos ajouter.
/// Passer par une interface permet de tester les écrans sans ouvrir de
/// boîte de dialogue.
/// </summary>
public interface IServiceFichier
{
    /// <summary>
    /// Renvoie le chemin choisi, ou <c>null</c> si l'utilisateur renonce.
    /// </summary>
    string? DemanderOuEnregistrer(string nomPropose, string filtre);

    /// <summary>Ouvre un fichier avec le programme associé de Windows.</summary>
    void Ouvrir(string chemin);

    /// <summary>
    /// Demande à l'utilisateur de choisir un ou plusieurs fichiers existants.
    /// Renvoie une liste vide s'il renonce.
    /// </summary>
    IReadOnlyList<string> Choisir(string titre, string filtre, bool plusieurs = true);
}
