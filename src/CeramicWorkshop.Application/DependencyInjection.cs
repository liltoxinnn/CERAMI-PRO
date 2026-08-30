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
        services.AddScoped<IProduitService, ProduitService>();
        services.AddScoped<IRecetteService, RecetteService>();
        services.AddScoped<IProductionService, ProductionService>();
        services.AddScoped<IFourService, FourService>();
        services.AddScoped<ICuissonService, CuissonService>();
        services.AddScoped<IDecorationService, DecorationService>();
        services.AddScoped<IQualiteService, QualiteService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ICommandeService, CommandeService>();
        services.AddScoped<IPaiementService, PaiementService>();
        services.AddScoped<IFactureService, FactureService>();
        services.AddScoped<IVenteService, VenteService>();
        services.AddScoped<IDepenseService, DepenseService>();
        services.AddScoped<ITableauDeBordService, TableauDeBordService>();
        services.AddScoped<IRapportService, RapportService>();
        services.AddScoped<ICalculateurService, CalculateurService>();
        services.AddScoped<ICodeService, CodeService>();

        return services;
    }
}
