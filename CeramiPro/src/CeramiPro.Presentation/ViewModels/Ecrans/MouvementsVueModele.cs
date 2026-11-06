using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Historique du stock : chaque entrée et chaque sortie, avec le stock avant
/// et après, et le document qui l'a provoquée. C'est ici que l'on explique
/// un écart, au lieu de le constater.
/// </summary>
public partial class MouvementsVueModele : ListeVueModele<MouvementStockDto>
{
    /// <summary>Valeur du filtre « tous les mouvements ».</summary>
    private const int Tous = -1;

    private readonly IInventaireService _service;

    public MouvementsVueModele(IInventaireService service, OutilsListe outils)
        : base(outils)
    {
        _service = service;

        Filtres = new FiltreListe[]
        {
            new FiltreChoix("Mouvement",
                new[] { new OptionChamp(Tous, "Tous les mouvements") }
                    .Concat(OptionChamp.De<InventoryTransactionType>())
                    .ToList(),
                FiltreChangeAsync, Tous)
        };
    }

    public override string Titre => "Historique du stock";

    public override string Introduction =>
        "Toutes les entrées et sorties de stock, des plus récentes aux plus anciennes. " +
        "Cherchez un produit par son nom ou sa référence.";

    public override IReadOnlyList<FiltreListe> Filtres { get; }

    protected override Task<PagedResult<MouvementStockDto>> LireAsync()
    {
        var type = ((FiltreChoix)Filtres[0]).Valeur;

        return _service.ListerAsync(new FiltreMouvementsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            TypeMouvement = type == Tous ? null : (InventoryTransactionType)type
        });
    }

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.DateHeure),
        new("Mouvement", "TypeMouvement", ColonneAlignement.Gauche),
        new("Produit", "Produit", ColonneAlignement.Gauche),
        new("Référence", "ReferenceProduit", ColonneAlignement.Gauche),
        new("Quantité", "Quantite", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Avant", "StockAvant", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Après", "StockApres", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Valeur", "CoutTotal", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Document", "Document", ColonneAlignement.Gauche),
        new("Par", "Utilisateur", ColonneAlignement.Gauche),
        new("Précision", "Notes", ColonneAlignement.Gauche)
    };
}
