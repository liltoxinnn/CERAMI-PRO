using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Contrôles effectués avant qu'une pièce devienne un produit fini.</summary>
public partial class QualiteVueModele : ListeVueModele<ControleQualiteDto>
{
    private readonly IQualiteService _service;

    public QualiteVueModele(IQualiteService service, IServiceLangue langue)
        : base(langue)
        => _service = service;

    public override string Titre => Langue["menu.qualite"];

    public override string Introduction => "Contrôles effectués avant qu'une pièce devienne un produit fini.";

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
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Production", "ProductionNumero", ColonneAlignement.Gauche),
        new("Date", "DateAffichee", ColonneAlignement.Gauche),
        new("Contrôlées", "QuantiteControlee", ColonneAlignement.Droite),
        new("Acceptées", "QuantiteAcceptee", ColonneAlignement.Droite),
        new("Refusées", "QuantiteRefusee", ColonneAlignement.Droite)
    };
}
