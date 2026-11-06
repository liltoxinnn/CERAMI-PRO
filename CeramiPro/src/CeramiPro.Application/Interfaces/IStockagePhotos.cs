namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Dossier des photos de l'atelier : produits et commandes.
///
/// Une photo choisie sur le disque est recopiée ici, si bien qu'effacer
/// l'original — dans « Téléchargements », sur le téléphone — ne fait rien
/// perdre. La base n'en retient que le chemin relatif à ce dossier : les
/// sauvegardes et un changement d'ordinateur restent ainsi possibles.
/// </summary>
public interface IStockagePhotos
{
    /// <summary>Dossier racine des photos.</summary>
    string Racine { get; }

    /// <summary>Recopie une photo dans le dossier et renvoie son chemin relatif.</summary>
    Task<string> ImporterAsync(string cheminSource, string sousDossier, CancellationToken cancellationToken = default);

    /// <summary>Chemin complet d'une photo, qu'elle soit enregistrée en relatif ou, pour les anciennes, en absolu.</summary>
    string CheminComplet(string chemin);

    /// <summary>Efface le fichier d'une photo, s'il se trouve dans le dossier des photos.</summary>
    void Supprimer(string chemin);

    /// <summary>Extensions acceptées, en minuscules, point compris.</summary>
    IReadOnlyCollection<string> ExtensionsAcceptees { get; }
}
