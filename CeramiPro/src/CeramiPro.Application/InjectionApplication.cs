using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Application;

/// <summary>
/// Enregistrement des services métier.
///
/// La validation des saisies est faite par les vues-modèles et par les règles
/// des services eux-mêmes : l'application de bureau n'a pas de filtre de
/// requêtes web à alimenter.
/// </summary>
public static class InjectionApplication
{
    public static IServiceCollection AjouterApplication(this IServiceCollection services)
    {

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
        services.AddScoped<IRechercheService, RechercheService>();
        services.AddScoped<IAlerteService, AlerteService>();

        return services;
    }
}
