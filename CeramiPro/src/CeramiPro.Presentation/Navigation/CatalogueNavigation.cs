using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Menu latéral de l'application, dans l'ordre voulu par l'atelier : on suit
/// le chemin de la matière première jusqu'à la facture.
///
/// Chaque entrée ouvre un écran réel, et porte le droit qu'il faut pour la
/// voir. Une entrée sans écran n'aurait aucune raison de figurer ici.
/// </summary>
public static class CatalogueNavigation
{
    public static IReadOnlyList<ElementNavigation> Construire(IServiceLangue langue)
    {
        ElementNavigation Entree(string cle, string icone, Type? destination = null,
            string? droit = null, IReadOnlyList<ElementNavigation>? enfants = null)
            => new(langue, cle, icone, destination, droit, enfants);

        return new List<ElementNavigation>
        {
            Entree("menu.tableauDeBord", "🏠", typeof(TableauDeBordVueModele),
                PermissionCodes.TableauDeBordConsulter),

            Entree("menu.caisse", "🧾", typeof(CaisseVueModele), PermissionCodes.VentesCreer),

            Entree("menu.stock", "📦", enfants: new List<ElementNavigation>
            {
                Entree("menu.stock.vueGenerale", "📊", typeof(StockVueModele),
                    PermissionCodes.MatieresConsulter),
                Entree("menu.stock.matieres", "🧱", typeof(MatieresVueModele),
                    PermissionCodes.MatieresConsulter),
                Entree("menu.stock.produitsFinis", "🏺", typeof(ProduitsStockVueModele),
                    PermissionCodes.ProduitsConsulter),
                Entree("menu.stock.mouvements", "🔄", typeof(MouvementsVueModele),
                    PermissionCodes.MouvementsConsulter),
                Entree("menu.stock.alertes", "⚠️", typeof(AlertesVueModele),
                    PermissionCodes.MatieresConsulter)
            }),

            Entree("menu.produits", "🏺", enfants: new List<ElementNavigation>
            {
                Entree("menu.produits.catalogue", "📖", typeof(ProduitsVueModele),
                    PermissionCodes.ProduitsConsulter),
                Entree("menu.produits.categories", "🗂️", typeof(CategoriesProduitsVueModele),
                    PermissionCodes.ProduitsConsulter),
                Entree("menu.produits.recettes", "🧪", typeof(RecettesVueModele),
                    PermissionCodes.RecettesConsulter),
                Entree("menu.produits.etiquettes", "🏷️", typeof(EtiquettesVueModele),
                    PermissionCodes.ProduitsConsulter)
            }),

            Entree("menu.production", "🏭", enfants: new List<ElementNavigation>
            {
                Entree("menu.production.tableau", "📅", typeof(TableauProductionVueModele),
                    PermissionCodes.ProductionConsulter),
                Entree("menu.production.ordres", "📋", typeof(ProductionVueModele),
                    PermissionCodes.ProductionConsulter),
                Entree("menu.production.enCours", "⚙️", typeof(ProductionEnCoursVueModele),
                    PermissionCodes.ProductionConsulter),
                Entree("menu.production.historique", "🕐", typeof(HistoriqueProductionVueModele),
                    PermissionCodes.ProductionConsulter)
            }),

            Entree("menu.cuisson", "🔥", enfants: new List<ElementNavigation>
            {
                Entree("menu.cuisson.fours", "🔥", typeof(FoursVueModele),
                    PermissionCodes.CuissonConsulter),
                Entree("menu.cuisson.lots", "📦", typeof(CuissonsVueModele),
                    PermissionCodes.CuissonConsulter),
                Entree("menu.cuisson.enfourner", "🧯", typeof(EnfournementVueModele),
                    PermissionCodes.CuissonGerer)
            }),

            Entree("menu.decoration", "🎨", enfants: new List<ElementNavigation>
            {
                Entree("menu.decoration.travaux", "🖌️", typeof(DecorationsVueModele),
                    PermissionCodes.DecorationConsulter),
                Entree("menu.decoration.types", "🗂️", typeof(TypesDecorationVueModele),
                    PermissionCodes.DecorationConsulter)
            }),

            Entree("menu.qualite", "🔍", typeof(QualiteVueModele), PermissionCodes.QualiteConsulter),
            Entree("menu.clients", "👥", typeof(ClientsVueModele), PermissionCodes.ClientsConsulter),
            Entree("menu.commandes", "📋", typeof(CommandesVueModele), PermissionCodes.CommandesConsulter),

            Entree("menu.fournisseurs", "🚚", enfants: new List<ElementNavigation>
            {
                Entree("menu.fournisseurs.fiches", "🚚", typeof(FournisseursVueModele),
                    PermissionCodes.FournisseursConsulter),
                Entree("menu.fournisseurs.achats", "🧾", typeof(AchatsVueModele),
                    PermissionCodes.AchatsConsulter),
                Entree("menu.fournisseurs.nouvelAchat", "➕", typeof(NouvelAchatVueModele),
                    PermissionCodes.AchatsGerer)
            }),

            Entree("menu.ventes", "🛒", typeof(VentesVueModele), PermissionCodes.VentesConsulter),
            Entree("menu.factures", "📄", typeof(FacturesVueModele), PermissionCodes.FacturesConsulter),
            Entree("menu.paiements", "💳", typeof(PaiementsVueModele), PermissionCodes.PaiementsConsulter),
            Entree("menu.depenses", "💸", typeof(DepensesVueModele), PermissionCodes.DepensesConsulter),
            Entree("menu.rapports", "📊", typeof(RapportsVueModele), PermissionCodes.RapportsConsulter),
            Entree("menu.calculateurs", "🧮", typeof(CalculateursVueModele)),

            Entree("menu.administration", "🛠️", enfants: new List<ElementNavigation>
            {
                Entree("menu.administration.utilisateurs", "👤", typeof(UtilisateursVueModele),
                    PermissionCodes.UtilisateursConsulter),
                Entree("menu.administration.unites", "📏", typeof(UnitesVueModele),
                    PermissionCodes.MatieresConsulter),
                Entree("menu.administration.categoriesMatieres", "🗂️",
                    typeof(CategoriesMatieresVueModele), PermissionCodes.MatieresConsulter),
                Entree("menu.administration.categoriesDepenses", "🗂️",
                    typeof(CategoriesDepensesVueModele), PermissionCodes.DepensesConsulter),
                Entree("menu.administration.sauvegarde", "💾", typeof(SauvegardeVueModele),
                    PermissionCodes.SauvegardeGerer)
            }),

            Entree("menu.parametres", "⚙️", typeof(ParametresVueModele),
                PermissionCodes.ParametresConsulter)
        };
    }
}
