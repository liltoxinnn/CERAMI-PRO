using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Achats de matières premières et réceptions en stock.</summary>
public partial class AchatsVueModele : ListeVueModele<AchatDto>
{
    private readonly IAchatService _service;

    public AchatsVueModele(IAchatService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    public override string Titre => "Achats";

    public override string Introduction => "Achats de matières premières et réceptions en stock. Un achat se saisit depuis l'écran « Nouvel achat ».";

    protected override Task<PagedResult<AchatDto>> LireAsync()
        => _service.ListerAsync(new FiltreAchatsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Fournisseur", "FournisseurNom", ColonneAlignement.Gauche),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Payé", "Paye", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
