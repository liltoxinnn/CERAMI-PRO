namespace CeramiPro.Application.Interfaces;

/// <summary>État de la connexion à la base, tel qu'il est présenté à l'écran.</summary>
public record EtatBaseDeDonnees(bool Disponible, string Message)
{
    public static EtatBaseDeDonnees Connectee(string nomBase)
        => new(true, $"Connectée à « {nomBase} »");

    public static EtatBaseDeDonnees Injoignable()
        => new(false, "Injoignable — vérifiez que PostgreSQL est démarré");
}

/// <summary>Vérification de la disponibilité de la base de données.</summary>
public interface IServiceEtatBaseDeDonnees
{
    Task<EtatBaseDeDonnees> VerifierAsync(CancellationToken cancellationToken = default);
}
