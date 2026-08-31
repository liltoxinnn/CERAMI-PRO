using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure.Data;
using CeramiPro.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure;

/// <summary>Enregistrement des services techniques : base de données, horloge, session.</summary>
public static class InjectionDependances
{
    public const string CleChaineConnexion = "CeramiProDB";

    public static IServiceCollection AjouterInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        var chaineConnexion = configuration.GetConnectionString(CleChaineConnexion)
            ?? throw new InvalidOperationException(
                $"La connexion à PostgreSQL est absente. Renseignez « ConnectionStrings:{CleChaineConnexion} » " +
                "dans appsettings.json.");

        services.AddDbContext<CeramiProDbContext>(options => options
            .UseNpgsql(chaineConnexion, npgsql => npgsql
                .MigrationsAssembly(typeof(CeramiProDbContext).Assembly.FullName)
                .EnableRetryOnFailure(3))
            .EnableDetailedErrors());

        // Une seule personne utilise l'application à la fois : sa session vit
        // aussi longtemps que le programme.
        services.AddSingleton<UtilisateurCourant>();
        services.AddSingleton<IUtilisateurCourant>(f => f.GetRequiredService<UtilisateurCourant>());

        services.AddSingleton<IServiceDateHeure>(fournisseur => new ServiceDateHeure(
            fournisseur.GetRequiredService<ILogger<ServiceDateHeure>>(),
            configuration["Atelier:FuseauHoraire"] ?? ParametresAtelier.FuseauHoraire));

        return services;
    }
}
