using System.Reflection;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Application.Services;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace CeramicWorkshop.Application;

/// <summary>Enregistrement des services métier dans le conteneur d'injection de dépendances.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly(), includeInternalTypes: true);

        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUtilisateurService, UtilisateurService>();
        services.AddScoped<IRoleService, RoleService>();
        services.AddScoped<IParametresService, ParametresService>();

        services.AddScoped<IReferenceNumberService, ReferenceNumberService>();
        services.AddScoped<IInventaireService, InventaireService>();
        services.AddScoped<IReferentielService, ReferentielService>();
        services.AddScoped<IUniteService, UniteService>();
        services.AddScoped<IMatiereService, MatiereService>();
        services.AddScoped<IFournisseurService, FournisseurService>();
        services.AddScoped<IAchatService, AchatService>();

        return services;
    }
}
