using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Électricité, gaz, transport, emballage, salaires.</summary>
public partial class DepensesVueModele : ListeVueModele<DepenseDto>
{
    private readonly IDepenseService _service;

    public DepensesVueModele(IDepenseService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(DepenseFormulaireVueModele);

    public override string Titre => Langue["menu.depenses"];

    public override string Introduction => "Électricité, gaz, transport, emballage, salaires.";

    protected override Task<PagedResult<DepenseDto>> LireAsync()
        => _service.ListerAsync(new FiltreDepensesRequete
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
        new("Catégorie", "CategorieNom", ColonneAlignement.Gauche),
        new("Description", "Description", ColonneAlignement.Gauche),
        new("Mode", "ModeReglement", ColonneAlignement.Gauche),
        new("Montant", "Montant", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
