using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fournées : four employé, température, durée et coût de l'énergie.</summary>
public partial class CuissonsVueModele : ListeVueModele<CuissonDto>
{
    private readonly ICuissonService _service;

    public CuissonsVueModele(ICuissonService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    public override string Titre => Langue["menu.cuisson.lots"];

    public override string Introduction => "Fournées : four employé, température, durée et coût de l'énergie. Un enfournement se saisit depuis l'écran « Enfourner ».";

    protected override Task<PagedResult<CuissonDto>> LireAsync()
        => _service.ListerAsync(new FiltreCuissonsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Four", "FourNom", ColonneAlignement.Gauche),
        new("Type", "TypeLibelle", ColonneAlignement.Gauche),
        new("Température", "Temperature", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Début", "Debut", ColonneAlignement.Gauche, FormatColonne.DateHeure),
        new("Durée (h)", "DureeHeures", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Pièces", "QuantiteTotale", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Cassées", "QuantiteEndommagee", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Coût énergie", "CoutEnergie", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
