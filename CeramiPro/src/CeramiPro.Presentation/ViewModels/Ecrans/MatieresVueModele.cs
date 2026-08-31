using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Argile, émail, pigments, emballage : stock, seuil d'alerte et coût moyen.</summary>
public partial class MatieresVueModele : ListeVueModele<MatiereDto>
{
    private readonly IMatiereService _service;

    public MatieresVueModele(IMatiereService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(MatiereFormulaireVueModele);

    public override bool PeutSupprimer => true;

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    public override string Titre => Langue["menu.stock.matieres"];

    public override string Introduction => "Argile, émail, pigments, emballage : stock, seuil d'alerte et coût moyen.";

    protected override Task<PagedResult<MatiereDto>> LireAsync()
        => _service.ListerAsync(new FiltreMatieresRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Catégorie", "CategorieNom", ColonneAlignement.Gauche),
        new("Unité", "UniteCode", ColonneAlignement.Centre),
        new("Stock", "QuantiteActuelle", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Seuil", "StockMinimum", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Coût moyen", "CoutMoyen", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Valeur", "ValeurStock", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
