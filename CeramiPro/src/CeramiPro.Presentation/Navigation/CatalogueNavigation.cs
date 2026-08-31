using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Menu latéral de l'application, dans l'ordre voulu par l'atelier : on suit
/// le chemin de la matière première jusqu'à la facture.
///
/// Les destinations non encore développées valent <c>null</c> : l'entrée
/// déplie son groupe ou reste inactive plutôt que d'ouvrir un écran vide.
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
            Entree("menu.tableauDeBord", "🏠", typeof(TableauDeBordVueModele)),

            Entree("menu.stock", "📦", enfants: new List<ElementNavigation>
            {
                Entree("menu.stock.vueGenerale", "📊"),
                Entree("menu.stock.matieres", "🧱", typeof(MatieresVueModele)),
                Entree("menu.stock.produitsFinis", "🏺"),
                Entree("menu.stock.mouvements", "🔄", typeof(MouvementsVueModele)),
                Entree("menu.stock.alertes", "⚠️")
            }),

            Entree("menu.produits", "🏺", enfants: new List<ElementNavigation>
            {
                Entree("menu.produits.catalogue", "📖", typeof(ProduitsVueModele)),
                Entree("menu.produits.categories", "🗂️"),
                Entree("menu.produits.variantes", "🎨"),
                Entree("menu.produits.recettes", "🧪")
            }),

            Entree("menu.production", "🏭", enfants: new List<ElementNavigation>
            {
                Entree("menu.production.ordres", "📋", typeof(ProductionVueModele)),
                Entree("menu.production.planning", "📅"),
                Entree("menu.production.enCours", "⚙️"),
                Entree("menu.production.historique", "🕐")
            }),

            Entree("menu.cuisson", "🔥", enfants: new List<ElementNavigation>
            {
                Entree("menu.cuisson.fours", "🔥"),
                Entree("menu.cuisson.lots", "📦", typeof(CuissonsVueModele)),
                Entree("menu.production.historique", "🕐")
            }),

            Entree("menu.decoration", "🎨", enfants: new List<ElementNavigation>
            {
                Entree("menu.decoration.travaux", "🖌️", typeof(DecorationsVueModele)),
                Entree("menu.decoration.types", "🗂️"),
                Entree("menu.production.historique", "🕐")
            }),

            Entree("menu.qualite", "🔍", typeof(QualiteVueModele)),
            Entree("menu.clients", "👥", typeof(ClientsVueModele)),
            Entree("menu.commandes", "📋", typeof(CommandesVueModele)),
            Entree("menu.fournisseurs", "🚚", typeof(FournisseursVueModele)),
            Entree("menu.ventes", "🛒", typeof(VentesVueModele)),
            Entree("menu.paiements", "💳", typeof(PaiementsVueModele)),
            Entree("menu.factures", "📄", typeof(FacturesVueModele)),
            Entree("menu.depenses", "💸", typeof(DepensesVueModele)),
            Entree("menu.rapports", "📊"),
            Entree("menu.parametres", "⚙️")
        };
    }
}
