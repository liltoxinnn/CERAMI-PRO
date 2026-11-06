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
        // Comptes et réglages
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUtilisateurService, UtilisateurService>();
        services.AddScoped<IParametresService, ParametresService>();
        services.AddScoped<IReferentielService, ReferentielService>();
        services.AddScoped<IReferenceNumberService, ReferenceNumberService>();

        // Produits et stock
        services.AddScoped<IProduitService, ProduitService>();
        services.AddScoped<IInventaireService, InventaireService>();

        // Clients, commandes et ventes
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<ICommandeService, CommandeService>();
        services.AddScoped<IVenteService, VenteService>();
        services.AddScoped<IPaiementService, PaiementService>();

        // Finances
        services.AddScoped<IDepenseService, DepenseService>();
        services.AddScoped<ITableauDeBordService, TableauDeBordService>();
        services.AddScoped<IRapportService, RapportService>();

        return services;
    }
}
