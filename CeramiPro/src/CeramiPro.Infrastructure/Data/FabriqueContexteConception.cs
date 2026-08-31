using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CeramiPro.Infrastructure.Data;

/// <summary>
/// Sert uniquement aux outils de migration en ligne de commande, qui doivent
/// pouvoir construire un contexte sans lancer l'application Windows.
/// </summary>
public class FabriqueContexteConception : IDesignTimeDbContextFactory<CeramiProDbContext>
{
    public CeramiProDbContext CreateDbContext(string[] args)
    {
        var chaine = Environment.GetEnvironmentVariable("CERAMIPRO_DB")
            ?? "Host=localhost;Port=5432;Database=CeramiProDB;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<CeramiProDbContext>()
            .UseNpgsql(chaine)
            .Options;

        return new CeramiProDbContext(options);
    }
}
