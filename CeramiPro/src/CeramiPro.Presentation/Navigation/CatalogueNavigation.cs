using CeramiPro.Presentation.ViewModels;
using CeramiPro.Presentation.ViewModels.Ecrans;

namespace CeramiPro.Presentation.Navigation;

/// <summary>
/// Menu latéral : les sept sections de l'atelier, et rien d'autre.
///
/// Chaque entrée ouvre un écran réel. Une section n'a de sous-entrées que
/// lorsqu'elle regroupe des écrans qui servent vraiment à des moments
/// différents — enregistrer une vente et relire l'historique, par exemple.
/// </summary>
public static class CatalogueNavigation
{
    public static IReadOnlyList<ElementNavigation> Construire()
    {
        static ElementNavigation Ecran<T>(string libelle, string icone, bool administrateur = false)
            => new(libelle, icone, typeof(T), administrateur);

        static ElementNavigation Section(string libelle, string icone, params ElementNavigation[] ecrans)
            => new(libelle, icone, enfants: ecrans);

        return new[]
        {
            Ecran<TableauDeBordVueModele>("Tableau de bord", "🏠"),

            Section("Produits", "🏺",
                Ecran<ProduitsVueModele>("Catalogue", "📖"),
                Ecran<ProduitsStockVueModele>("Stock", "📦"),
                Ecran<MouvementsVueModele>("Historique du stock", "🔄"),
                Ecran<CategoriesProduitsVueModele>("Catégories", "🗂️")),

            Ecran<CommandesVueModele>("Commandes", "📋"),

            Ecran<ClientsVueModele>("Clients", "👥"),

            Section("Ventes", "🛒",
                Ecran<CaisseVueModele>("Nouvelle vente", "🧾"),
                Ecran<VentesVueModele>("Historique des ventes", "📜"),
                Ecran<PaiementsVueModele>("Paiements", "💳")),

            Section("Rapports", "📊",
                Ecran<RapportsVueModele>("Rapports", "📈"),
                Ecran<DepensesVueModele>("Dépenses", "💸")),

            Section("Paramètres", "⚙️",
                Ecran<ParametresVueModele>("Atelier", "🏛️"),
                Ecran<CategoriesDepensesVueModele>("Catégories de dépenses", "🗂️"),
                Ecran<UtilisateursVueModele>("Utilisateurs", "👤", administrateur: true),
                Ecran<SauvegardeVueModele>("Sauvegarde", "💾", administrateur: true))
        };
    }
}
