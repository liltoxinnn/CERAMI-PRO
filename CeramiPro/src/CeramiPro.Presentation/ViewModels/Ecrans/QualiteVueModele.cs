using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Contrôles qualité effectués avant emballage.</summary>
public partial class QualiteVueModele : ListeVueModele<ControleQualiteDto>
{
    private readonly IQualiteService _service;

    public QualiteVueModele(IQualiteService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    public override string Titre => Langue["menu.qualite"];

    public override string Introduction => "Contrôles qualité effectués avant emballage : pièces acceptées, refusées et à retoucher.";

    protected override Task<PagedResult<ControleQualiteDto>> LireAsync()
        => _service.ListerAsync(new FiltreControlesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference", ColonneAlignement.Gauche),
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Production", "ProductionNumero", ColonneAlignement.Gauche),
        new("Contrôleur", "Controleur", ColonneAlignement.Gauche),
        new("Contrôlées", "QuantiteControlee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Acceptées", "QuantiteAcceptee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Refusées", "QuantiteRefusee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("À retoucher", "QuantiteARetoucher", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Résultat", "ResultatLibelle", ColonneAlignement.Gauche)
    };
}
