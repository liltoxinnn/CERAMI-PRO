using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.ViewModels;

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
                Entree("menu.stock.matieres", "🧱"),
                Entree("menu.stock.produitsFinis", "🏺"),
                Entree("menu.stock.mouvements", "🔄"),
                Entree("menu.stock.alertes", "⚠️")
            }),

            Entree("menu.produits", "🏺", enfants: new List<ElementNavigation>
            {
                Entree("menu.produits.catalogue", "📖"),
                Entree("menu.produits.categories", "🗂️"),
                Entree("menu.produits.variantes", "🎨"),
                Entree("menu.produits.recettes", "🧪")
            }),

            Entree("menu.production", "🏭", enfants: new List<ElementNavigation>
            {
                Entree("menu.production.ordres", "📋"),
                Entree("menu.production.planning", "📅"),
                Entree("menu.production.enCours", "⚙️"),
                Entree("menu.production.historique", "🕐")
            }),

            Entree("menu.cuisson", "🔥", enfants: new List<ElementNavigation>
            {
                Entree("menu.cuisson.fours", "🔥"),
                Entree("menu.cuisson.lots", "📦"),
                Entree("menu.production.historique", "🕐")
            }),

            Entree("menu.decoration", "🎨", enfants: new List<ElementNavigation>
            {
                Entree("menu.decoration.travaux", "🖌️"),
                Entree("menu.decoration.types", "🗂️"),
                Entree("menu.production.historique", "🕐")
            }),

            Entree("menu.qualite", "🔍"),
            Entree("menu.clients", "👥"),
            Entree("menu.commandes", "📋"),
            Entree("menu.fournisseurs", "🚚"),
            Entree("menu.ventes", "🛒"),
            Entree("menu.paiements", "💳"),
            Entree("menu.factures", "📄"),
            Entree("menu.depenses", "💸"),
            Entree("menu.rapports", "📊"),
            Entree("menu.parametres", "⚙️")
        };
    }
}
