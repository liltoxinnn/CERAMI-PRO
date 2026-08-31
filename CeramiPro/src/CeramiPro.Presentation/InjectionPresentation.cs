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

        services.AddSingleton<FenetrePrincipaleVueModele>();
        services.AddTransient<TableauDeBordVueModele>();
        services.AddTransient<ConnexionVueModele>();
        services.AddTransient<ChangementMotDePasseVueModele>();

        AjouterEcrans(services);
        AjouterFormulaires(services);

        return services;
    }

    /// <summary>
    /// Écrans de l'application. Chaque entrée du menu latéral pointe vers
    /// l'une de ces vues-modèles.
    /// </summary>
    private static void AjouterEcrans(IServiceCollection services)
    {
        // Stock et catalogue
        services.AddTransient<StockVueModele>();
        services.AddTransient<MatieresVueModele>();
        services.AddTransient<ProduitsVueModele>();
        services.AddTransient<ProduitsStockVueModele>();
        services.AddTransient<MouvementsVueModele>();
        services.AddTransient<AlertesVueModele>();
        services.AddTransient<RecettesVueModele>();
        services.AddTransient<EtiquettesVueModele>();

        // Production, cuisson, décoration, qualité
        services.AddTransient<TableauProductionVueModele>();
        services.AddTransient<ProductionVueModele>();
        services.AddTransient<ProductionEnCoursVueModele>();
        services.AddTransient<HistoriqueProductionVueModele>();
        services.AddTransient<FoursVueModele>();
        services.AddTransient<CuissonsVueModele>();
        services.AddTransient<EnfournementVueModele>();
        services.AddTransient<DecorationsVueModele>();
        services.AddTransient<QualiteVueModele>();

        // Clients, fournisseurs et commerce
        services.AddTransient<ClientsVueModele>();
        services.AddTransient<CommandesVueModele>();
        services.AddTransient<FournisseursVueModele>();
        services.AddTransient<AchatsVueModele>();
        services.AddTransient<NouvelAchatVueModele>();
        services.AddTransient<CaisseVueModele>();
        services.AddTransient<VentesVueModele>();
        services.AddTransient<FacturesVueModele>();
        services.AddTransient<PaiementsVueModele>();
        services.AddTransient<DepensesVueModele>();

        // Rapports et outils
        services.AddTransient<RapportsVueModele>();
        services.AddTransient<CalculateursVueModele>();

        // Administration
        services.AddTransient<UtilisateursVueModele>();
        services.AddTransient<UnitesVueModele>();
        services.AddTransient<CategoriesMatieresVueModele>();
        services.AddTransient<CategoriesProduitsVueModele>();
        services.AddTransient<CategoriesDepensesVueModele>();
        services.AddTransient<TypesDecorationVueModele>();
        services.AddTransient<SauvegardeVueModele>();
        services.AddTransient<ParametresVueModele>();
    }

    /// <summary>
    /// Formulaires de saisie. Un écran de liste demande le sien à l'injection
    /// de dépendances au moment d'ouvrir une fiche.
    /// </summary>
    private static void AjouterFormulaires(IServiceCollection services)
    {
        services.AddTransient<MatiereFormulaireVueModele>();
        services.AddTransient<ProduitFormulaireVueModele>();
        services.AddTransient<FournisseurFormulaireVueModele>();
        services.AddTransient<ClientFormulaireVueModele>();
        services.AddTransient<CommandeFormulaireVueModele>();
        services.AddTransient<PaiementFormulaireVueModele>();
        services.AddTransient<DepenseFormulaireVueModele>();
        services.AddTransient<OrdreProductionFormulaireVueModele>();
        services.AddTransient<FourFormulaireVueModele>();
        services.AddTransient<UniteFormulaireVueModele>();
        services.AddTransient<UtilisateurFormulaireVueModele>();
        services.AddTransient<CategorieMatiereFormulaireVueModele>();
        services.AddTransient<CategorieProduitFormulaireVueModele>();
        services.AddTransient<CategorieDepenseFormulaireVueModele>();
        services.AddTransient<TypeDecorationFormulaireVueModele>();
    }
}
