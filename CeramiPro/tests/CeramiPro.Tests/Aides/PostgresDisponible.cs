using Npgsql;

namespace CeramiPro.Tests.Aides;

/// <summary>
/// Certains tests ont besoin d'un vrai serveur PostgreSQL. Quand il est
/// absent, ils sont ignorés plutôt que déclarés en échec : une machine sans
/// base ne doit pas faire croire à une régression.
/// </summary>
public static class PostgresDisponible
{
    public static string ChaineConnexion { get; } =
        Environment.GetEnvironmentVariable("CERAMIPRO_TEST_DB")
        ?? "Host=localhost;Port=5432;Database=CeramiProDB_Tests;Username=postgres;Password=postgres";

    private static readonly Lazy<bool> _joignable = new(() =>
    {
        try
        {
            var constructeur = new NpgsqlConnectionStringBuilder(ChaineConnexion)
            {
                Database = "postgres",
                Timeout = 3
            };

            using var connexion = new NpgsqlConnection(constructeur.ConnectionString);
            connexion.Open();
            return true;
        }
        catch
        {
            return false;
        }
    });

    public static bool Joignable => _joignable.Value;
}

/// <summary>Test exécuté seulement si PostgreSQL répond.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!PostgresDisponible.Joignable)
        {
            Skip = "PostgreSQL n'est pas joignable : test ignoré.";
        }
    }
}

/// <summary>
/// Regroupe les tests qui créent et suppriment la base. xUnit exécute les
/// classes en parallèle : sans ce regroupement, deux tests se disputeraient
/// la même base de données.
/// </summary>
[CollectionDefinition(Nom, DisableParallelization = true)]
public class CollectionPostgres
{
    public const string Nom = "PostgreSQL";
}
