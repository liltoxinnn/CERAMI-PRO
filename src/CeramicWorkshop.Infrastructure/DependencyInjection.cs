using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Infrastructure.Authentication;
using CeramicWorkshop.Infrastructure.Data;
using CeramicWorkshop.Infrastructure.Data.Seed;
using CeramicWorkshop.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CeramicWorkshop.Infrastructure;

/// <summary>Enregistrement de l'accès aux données et des services techniques.</summary>
public static class DependencyInjection
{
    /// <summary>Nom de la chaîne de connexion PostgreSQL attendue dans la configuration.</summary>
    public const string NomChaineConnexion = "CeramicWorkshopDB";

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var chaineConnexion = configuration.GetConnectionString(NomChaineConnexion)
            ?? throw new InvalidOperationException(
                $"La chaîne de connexion « {NomChaineConnexion} » est absente de la configuration. " +
                "Renseignez-la dans appsettings.json ou dans une variable d'environnement.");

        services.AddDbContext<ApplicationDbContext>(options =>
        {
            options.UseNpgsql(chaineConnexion, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(3);
            });
        });

        services.AddScoped<IApplicationDbContext>(fournisseur => fournisseur.GetRequiredService<ApplicationDbContext>());

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.Section));

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IPasswordHasherService, PasswordHasherService>();
        services.AddSingleton<ITokenService, TokenService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddSingleton<ICodeGraphiqueService, CodeGraphiqueService>();
        services.AddScoped<ISauvegardeService, SauvegardeService>();
        services.AddHostedService<SauvegardeAutomatique>();

        services.AddSingleton<IDateTimeService>(fournisseur => new DateTimeService(
            fournisseur.GetRequiredService<ILogger<DateTimeService>>(),
            configuration["Atelier:FuseauHoraire"]));

        services.AddScoped<DatabaseSeeder>();

        return services;
    }
}
