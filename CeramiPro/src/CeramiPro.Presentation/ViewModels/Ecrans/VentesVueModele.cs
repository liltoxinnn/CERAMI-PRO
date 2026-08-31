using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Ventes au comptoir et sur commande. Une vente se saisit depuis la caisse.</summary>
public partial class VentesVueModele : ListeVueModele<VenteDto>
{
    private readonly IVenteService _service;

    public VentesVueModele(IVenteService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    public override string Titre => Langue["menu.ventes"];

    public override string Introduction => "Ventes au comptoir et sur commande. Une vente se saisit depuis l'écran « Caisse ».";

    protected override Task<PagedResult<VenteDto>> LireAsync()
        => _service.ListerAsync(new FiltreVentesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Date", "Date", ColonneAlignement.Gauche, FormatColonne.DateHeure),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Payé", "Paye", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Bénéfice", "Benefice", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
