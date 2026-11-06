namespace CeramiPro.Application.Interfaces;

/// <summary>État de la connexion à la base, tel qu'il est présenté à l'écran.</summary>
public record EtatBaseDeDonnees(bool Disponible, string Message)
{
    public static EtatBaseDeDonnees Connectee(string nomBase)
        => new(true, $"Connectée à « {nomBase} »");

    public static EtatBaseDeDonnees Injoignable()
        => new(false, "Injoignable — vérifiez que PostgreSQL est démarré");
}

/// <summary>
/// Ce que l'écran « Paramètres » montre de la base de données. Le mot de
/// passe n'y figure jamais : il reste dans le fichier de configuration.
/// </summary>
public record InformationsBaseDeDonnees(
    bool Disponible,
    string Serveur,
    int Port,
    string NomBase,
    string Compte,
    string? VersionPostgres,
    string? VersionSchema,
    int MisesAJourEnAttente,
    int TablesArchivees,
    string FichierConfiguration);

/// <summary>Vérification de la disponibilité de la base de données.</summary>
public interface IServiceEtatBaseDeDonnees
{
    Task<EtatBaseDeDonnees> VerifierAsync(CancellationToken cancellationToken = default);

    /// <summary>Connexion, version et état du schéma, pour l'écran des paramètres.</summary>
    Task<InformationsBaseDeDonnees> DecrireAsync(CancellationToken cancellationToken = default);
}
