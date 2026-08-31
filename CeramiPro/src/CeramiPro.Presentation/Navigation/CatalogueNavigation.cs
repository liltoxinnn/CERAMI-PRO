using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Menu latéral de l'application, dans l'ordre voulu par l'atelier : on suit
/// le chemin de la matière première jusqu'à la facture.
///
/// Les destinations non encore développées valent <c>null</c> : l'entrée
/// apparaît alors désactivée plutôt que d'ouvrir un écran vide.
/// </summary>
public static class CatalogueNavigation
{
    public static IReadOnlyList<ElementNavigation> Construire() => new List<ElementNavigation>
    {
        new("Tableau de bord", "🏠", typeof(TableauDeBordVueModele)),

        new("Stock", "📦", enfants: new List<ElementNavigation>
        {
            new("Vue générale", "📊"),
            new("Matières premières", "🧱"),
            new("Produits finis", "🏺"),
            new("Mouvements", "🔄"),
            new("Alertes", "⚠️")
        }),

        new("Produits", "🏺", enfants: new List<ElementNavigation>
        {
            new("Catalogue", "📖"),
            new("Catégories", "🗂️"),
            new("Variantes", "🎨"),
            new("Recettes", "🧪")
        }),

        new("Production", "🏭", enfants: new List<ElementNavigation>
        {
            new("Ordres de production", "📋"),
            new("Planning", "📅"),
            new("Production en cours", "⚙️"),
            new("Historique", "🕐")
        }),

        new("Cuisson", "🔥", enfants: new List<ElementNavigation>
        {
            new("Fours", "🔥"),
            new("Lots de cuisson", "📦"),
            new("Historique", "🕐")
        }),

        new("Décoration", "🎨", enfants: new List<ElementNavigation>
        {
            new("Travaux de décoration", "🖌️"),
            new("Types de décoration", "🗂️"),
            new("Historique", "🕐")
        }),

        new("Contrôle qualité", "🔍"),
        new("Clients", "👥"),
        new("Commandes personnalisées", "📋"),
        new("Fournisseurs", "🚚"),
        new("Ventes", "🛒"),
        new("Paiements", "💳"),
        new("Factures", "📄"),
        new("Dépenses", "💸"),
        new("Rapports", "📊"),
        new("Paramètres", "⚙️")
    };
}
