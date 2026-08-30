using Npgsql;

namespace CeramicWorkshop.IntegrationTests.Aides;

/// <summary>
/// Vérifie qu'un serveur PostgreSQL est joignable. Sans serveur, les tests
/// d'intégration sont ignorés plutôt que signalés en échec.
/// </summary>
public static class PostgresDisponible
{
    /// <summary>Variable d'environnement permettant de viser un autre serveur.</summary>
    public const string VariableEnvironnement = "CERAMIPRO_TEST_DB";

    public static string ChaineConnexion =>
        Environment.GetEnvironmentVariable(VariableEnvironnement)
        ?? "Host=localhost;Port=5432;Database=CeramicWorkshopDB_Tests;Username=postgres;Password=postgres";

    private static readonly Lazy<bool> Verification = new(() =>
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
        catch (Exception)
        {
            return false;
        }
    });

    public static bool Joignable => Verification.Value;

    public const string RaisonIgnore =
        "Aucun serveur PostgreSQL joignable : définissez CERAMIPRO_TEST_DB pour exécuter ces tests.";
}

/// <summary>Test exécuté uniquement lorsqu'un serveur PostgreSQL est disponible.</summary>
public sealed class PostgresFactAttribute : FactAttribute
{
    public PostgresFactAttribute()
    {
        if (!PostgresDisponible.Joignable)
        {
            Skip = PostgresDisponible.RaisonIgnore;
        }
    }
}
