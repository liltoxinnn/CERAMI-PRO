namespace CeramiPro.Domain.Common;

/// <summary>
/// Catalogue des droits du logiciel. Chaque droit porte un libellé français
/// affiché dans l'écran de gestion des rôles.
/// </summary>
public static class PermissionCodes
{
    // Tableau de bord
    public const string TableauDeBordConsulter = "tableau-de-bord.consulter";

    // Stock
    public const string MatieresConsulter = "matieres.consulter";
    public const string MatieresGerer = "matieres.gerer";
    public const string MouvementsConsulter = "mouvements.consulter";
    public const string MouvementsGerer = "mouvements.gerer";

    // Produits
    public const string ProduitsConsulter = "produits.consulter";
    public const string ProduitsGerer = "produits.gerer";
    public const string RecettesConsulter = "recettes.consulter";
    public const string RecettesGerer = "recettes.gerer";

    // Production
    public const string ProductionConsulter = "production.consulter";
    public const string ProductionGerer = "production.gerer";
    public const string ProductionChangerEtape = "production.changer-etape";
    public const string ProductionDeroger = "production.deroger";
    public const string CuissonConsulter = "cuisson.consulter";
    public const string CuissonGerer = "cuisson.gerer";
    public const string DecorationConsulter = "decoration.consulter";
    public const string DecorationGerer = "decoration.gerer";
    public const string QualiteConsulter = "qualite.consulter";
    public const string QualiteControler = "qualite.controler";

    // Clients et commandes
    public const string ClientsConsulter = "clients.consulter";
    public const string ClientsGerer = "clients.gerer";
    public const string CommandesConsulter = "commandes.consulter";
    public const string CommandesGerer = "commandes.gerer";

    // Fournisseurs et achats
    public const string FournisseursConsulter = "fournisseurs.consulter";
    public const string FournisseursGerer = "fournisseurs.gerer";
    public const string AchatsConsulter = "achats.consulter";
    public const string AchatsGerer = "achats.gerer";

    // Ventes, factures et paiements
    public const string VentesConsulter = "ventes.consulter";
    public const string VentesCreer = "ventes.creer";
    public const string VentesAnnuler = "ventes.annuler";
    public const string FacturesConsulter = "factures.consulter";
    public const string FacturesEmettre = "factures.emettre";
    public const string PaiementsConsulter = "paiements.consulter";
    public const string PaiementsEnregistrer = "paiements.enregistrer";
    public const string PaiementsAnnuler = "paiements.annuler";

    // Dépenses et rapports
    public const string DepensesConsulter = "depenses.consulter";
    public const string DepensesGerer = "depenses.gerer";
    public const string RapportsConsulter = "rapports.consulter";
    public const string RapportsExporter = "rapports.exporter";

    // Administration
    public const string UtilisateursConsulter = "utilisateurs.consulter";
    public const string UtilisateursGerer = "utilisateurs.gerer";
    public const string ParametresConsulter = "parametres.consulter";
    public const string ParametresModifier = "parametres.modifier";
    public const string AuditConsulter = "audit.consulter";
    public const string SauvegardeGerer = "sauvegarde.gerer";

    public static readonly IReadOnlyList<PermissionDefinition> Catalogue = new List<PermissionDefinition>
    {
        new(TableauDeBordConsulter, "Consulter le tableau de bord", "Tableau de bord"),

        new(MatieresConsulter, "Consulter les matières premières", "Stock"),
        new(MatieresGerer, "Gérer les matières premières", "Stock"),
        new(MouvementsConsulter, "Consulter les mouvements de stock", "Stock"),
        new(MouvementsGerer, "Enregistrer un mouvement de stock", "Stock"),

        new(ProduitsConsulter, "Consulter les produits", "Produits"),
        new(ProduitsGerer, "Gérer les produits", "Produits"),
        new(RecettesConsulter, "Consulter les recettes", "Produits"),
        new(RecettesGerer, "Gérer les recettes", "Produits"),

        new(ProductionConsulter, "Consulter la production", "Production"),
        new(ProductionGerer, "Gérer les ordres de production", "Production"),
        new(ProductionChangerEtape, "Faire avancer les étapes de fabrication", "Production"),
        new(ProductionDeroger, "Autoriser une dérogation de stock", "Production"),
        new(CuissonConsulter, "Consulter les cuissons", "Production"),
        new(CuissonGerer, "Gérer les cuissons et les fours", "Production"),
        new(DecorationConsulter, "Consulter la décoration", "Production"),
        new(DecorationGerer, "Gérer la décoration", "Production"),
        new(QualiteConsulter, "Consulter le contrôle qualité", "Production"),
        new(QualiteControler, "Réaliser un contrôle qualité", "Production"),

        new(ClientsConsulter, "Consulter les clients", "Clients"),
        new(ClientsGerer, "Gérer les clients", "Clients"),
        new(CommandesConsulter, "Consulter les commandes personnalisées", "Clients"),
        new(CommandesGerer, "Gérer les commandes personnalisées", "Clients"),

        new(FournisseursConsulter, "Consulter les fournisseurs", "Fournisseurs"),
        new(FournisseursGerer, "Gérer les fournisseurs", "Fournisseurs"),
        new(AchatsConsulter, "Consulter les achats", "Fournisseurs"),
        new(AchatsGerer, "Gérer les achats", "Fournisseurs"),

        new(VentesConsulter, "Consulter les ventes", "Ventes"),
        new(VentesCreer, "Enregistrer une vente", "Ventes"),
        new(VentesAnnuler, "Annuler une vente", "Ventes"),
        new(FacturesConsulter, "Consulter les factures", "Ventes"),
        new(FacturesEmettre, "Émettre une facture", "Ventes"),
        new(PaiementsConsulter, "Consulter les paiements", "Paiements"),
        new(PaiementsEnregistrer, "Enregistrer un paiement", "Paiements"),
        new(PaiementsAnnuler, "Annuler un paiement", "Paiements"),

        new(DepensesConsulter, "Consulter les dépenses", "Dépenses"),
        new(DepensesGerer, "Gérer les dépenses", "Dépenses"),
        new(RapportsConsulter, "Consulter les rapports", "Rapports"),
        new(RapportsExporter, "Exporter les rapports", "Rapports"),

        new(UtilisateursConsulter, "Consulter les utilisateurs", "Administration"),
        new(UtilisateursGerer, "Gérer les utilisateurs et les rôles", "Administration"),
        new(ParametresConsulter, "Consulter les paramètres", "Administration"),
        new(ParametresModifier, "Modifier les paramètres", "Administration"),
        new(AuditConsulter, "Consulter le journal des opérations", "Administration"),
        new(SauvegardeGerer, "Gérer les sauvegardes", "Administration")
    };

    /// <summary>Droits accordés par défaut à chaque rôle lors de l'installation.</summary>
    public static readonly IReadOnlyDictionary<string, IReadOnlyList<string>> DroitsParDefaut =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [RoleCodes.Administrateur] = Catalogue.Select(p => p.Code).ToList(),

            [RoleCodes.Responsable] = new[]
            {
                TableauDeBordConsulter,
                MatieresConsulter, MatieresGerer, MouvementsConsulter, MouvementsGerer,
                ProduitsConsulter, ProduitsGerer, RecettesConsulter, RecettesGerer,
                ProductionConsulter, ProductionGerer, ProductionChangerEtape,
                CuissonConsulter, CuissonGerer, DecorationConsulter, DecorationGerer,
                QualiteConsulter, QualiteControler,
                ClientsConsulter, ClientsGerer, CommandesConsulter, CommandesGerer,
                FournisseursConsulter, FournisseursGerer, AchatsConsulter, AchatsGerer,
                VentesConsulter, VentesCreer, VentesAnnuler,
                FacturesConsulter, FacturesEmettre,
                PaiementsConsulter, PaiementsEnregistrer,
                DepensesConsulter, DepensesGerer,
                RapportsConsulter, RapportsExporter,
                ParametresConsulter
            },

            [RoleCodes.Employe] = new[]
            {
                TableauDeBordConsulter,
                MatieresConsulter, MouvementsConsulter,
                ProduitsConsulter, RecettesConsulter,
                ProductionConsulter, ProductionChangerEtape,
                CuissonConsulter, CuissonGerer,
                DecorationConsulter, DecorationGerer,
                QualiteConsulter, QualiteControler,
                CommandesConsulter
            },

            [RoleCodes.Caissier] = new[]
            {
                TableauDeBordConsulter,
                ProduitsConsulter,
                ClientsConsulter, ClientsGerer,
                CommandesConsulter,
                VentesConsulter, VentesCreer,
                FacturesConsulter, FacturesEmettre,
                PaiementsConsulter, PaiementsEnregistrer
            }
        };
}
