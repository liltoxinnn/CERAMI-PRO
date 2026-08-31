using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fournisseurs de matières premières, achats et règlements.</summary>
public partial class FournisseursVueModele : ListeVueModele<FournisseurDto>
{
    private readonly IFournisseurService _service;

    public FournisseursVueModele(IFournisseurService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(FournisseurFormulaireVueModele);

    public override bool PeutSupprimer => true;

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    public override string Titre => Langue["menu.fournisseurs"];

    public override string Introduction => "Fournisseurs de matières premières, achats et règlements.";

    protected override Task<PagedResult<FournisseurDto>> LireAsync()
        => _service.ListerAsync(new FiltreFournisseursRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Entreprise", "Entreprise", ColonneAlignement.Gauche),
        new("Téléphone", "Telephone", ColonneAlignement.Gauche),
        new("Ville", "Ville", ColonneAlignement.Gauche),
        new("Total des achats", "TotalAchats", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste dû", "Reste", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
