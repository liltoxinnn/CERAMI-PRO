using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Toutes les entrées et sorties de stock, dans l'ordre où elles ont eu lieu.</summary>
public partial class MouvementsVueModele : ListeVueModele<MouvementStockDto>
{
    private readonly IInventaireService _service;

    public MouvementsVueModele(IInventaireService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    public override string Titre => Langue["menu.stock.mouvements"];

    public override string Introduction => "Toutes les entrées et sorties de stock, dans l'ordre où elles ont eu lieu.";

    protected override Task<PagedResult<MouvementStockDto>> LireAsync()
        => _service.ListerAsync(new FiltreMouvementsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.DateHeure),
        new("Type", "TypeMouvement", ColonneAlignement.Gauche),
        new("Article", "Article", ColonneAlignement.Gauche),
        new("Quantité", "Quantite", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Unité", "Unite", ColonneAlignement.Centre),
        new("Avant", "StockAvant", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Après", "StockApres", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Document", "Document", ColonneAlignement.Gauche),
        new("Utilisateur", "Utilisateur", ColonneAlignement.Gauche)
    };
}
