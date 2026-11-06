using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Presentation.ViewModels.Formulaires;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.Presentation;

/// <summary>
/// Enregistrement des écrans et des formulaires.
///
/// Cette déclaration vit ici plutôt que dans l'application Windows afin
/// d'être vérifiée par les tests : un écran atteignable depuis le menu mais
/// absent de l'injection de dépendances ne se verrait ni à la compilation,
/// ni dans les tests métier — seulement au clic, devant l'utilisateur.
/// </summary>
public static class InjectionPresentation
{
    public static IServiceCollection AjouterPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IServiceNavigation, ServiceNavigation>();

        // Les services communs à tous les écrans de liste, réunis pour éviter
        // cinq paramètres au constructeur de chacun.
        services.AddTransient<OutilsListe>();
        services.AddTransient<FabriqueGaleries>();
        services.AddTransient<FabriqueFicheClient>();

        services.AddSingleton<FenetrePrincipaleVueModele>();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddTransient<ConnexionVueModele>();
        services.AddTransient<ChangementMotDePasseVueModele>();

        AjouterEcrans(services);
        AjouterFormulaires(services);

        return services;
    }

    /// <summary>
    /// Écrans de l'application, rangés comme les sept sections du menu.
    /// Chaque entrée du menu latéral pointe vers l'une de ces vues-modèles.
    /// </summary>
    private static void AjouterEcrans(IServiceCollection services)
    {
        // Produits
        services.AddTransient<ProduitsVueModele>();
        services.AddTransient<ProduitsStockVueModele>();
        services.AddTransient<MouvementsVueModele>();
        services.AddTransient<CategoriesProduitsVueModele>();

        // Commandes et clients
        services.AddTransient<CommandesVueModele>();
        services.AddTransient<ClientsVueModele>();

        // Ventes
        services.AddTransient<CaisseVueModele>();
        services.AddTransient<VentesVueModele>();
        services.AddTransient<PaiementsVueModele>();

        // Rapports
        services.AddTransient<RapportsVueModele>();
        services.AddTransient<DepensesVueModele>();

        // Paramètres
        services.AddTransient<ParametresVueModele>();
        services.AddTransient<CategoriesDepensesVueModele>();
        services.AddTransient<UtilisateursVueModele>();
        services.AddTransient<SauvegardeVueModele>();
    }

    /// <summary>
    /// Formulaires de saisie. Un écran de liste demande le sien à l'injection
    /// de dépendances au moment d'ouvrir une fiche.
    /// </summary>
    private static void AjouterFormulaires(IServiceCollection services)
    {
        services.AddTransient<ProduitFormulaireVueModele>();
        services.AddTransient<MouvementStockFormulaireVueModele>();
        services.AddTransient<RegularisationFormulaireVueModele>();
        services.AddTransient<CategorieProduitFormulaireVueModele>();
        services.AddTransient<ClientFormulaireVueModele>();
        services.AddTransient<CommandeFormulaireVueModele>();
        services.AddTransient<PaiementFormulaireVueModele>();
        services.AddTransient<DepenseFormulaireVueModele>();
        services.AddTransient<CategorieDepenseFormulaireVueModele>();
        services.AddTransient<UtilisateurFormulaireVueModele>();
    }
}
