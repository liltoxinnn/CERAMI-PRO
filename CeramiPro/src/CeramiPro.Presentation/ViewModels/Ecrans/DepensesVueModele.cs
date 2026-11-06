using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Électricité, gaz, transport, emballage, salaires.</summary>
public partial class DepensesVueModele : ListeVueModele<DepenseDto>
{
    private readonly IDepenseService _service;

    public DepensesVueModele(IDepenseService service, OutilsListe outils)
        : base(outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(DepenseFormulaireVueModele);

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Supprimer la dépense", SupprimerDepenseCommand, Destructive: true,
            Aide: "Pour une dépense saisie par erreur. Elle reste consultable dans l'historique.")
    };

    [RelayCommand]
    private Task SupprimerDepenseAsync() => AgirAvecMotifAsync(
        (depense, motif) => _service.SupprimerAsync(depense.Id, motif),
        question: "Pourquoi supprimer cette dépense ?\n\nElle disparaîtra des rapports ; le motif reste dans l'historique.",
        succes: "La dépense est supprimée.");

    public override string Titre => "Dépenses";

    public override string Introduction =>
        "Ce que coûte l'atelier : argile et émaux, électricité et gaz du four, emballages, transport, " +
        "salaires. Ces dépenses sont déduites dans le rapport des bénéfices.";

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
