using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Travaux de décoration : émaillage, peinture, dorure.</summary>
public partial class DecorationsVueModele : ListeVueModele<DecorationDto>
{
    private readonly IDecorationService _service;

    public DecorationsVueModele(IDecorationService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    /// <summary>Un travail de décoration se démarre, puis se termine.</summary>
    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Démarrer le travail", DemarrerCommand),
        new("Marquer comme terminé", TerminerCommand)
    };

    [RelayCommand]
    private Task DemarrerAsync() => AgirAsync(
        decoration => _service.ChangerStatutAsync(decoration.Id, DecorationStatus.EnCours),
        succes: "Le travail de décoration a démarré.");

    [RelayCommand]
    private Task TerminerAsync() => AgirAsync(
        decoration => _service.ChangerStatutAsync(decoration.Id, DecorationStatus.Terminee),
        confirmation: "Marquer ce travail de décoration comme terminé ?",
        succes: "Le travail est terminé.");

    public override string Titre => Langue["menu.decoration.travaux"];

    public override string Introduction => "Travaux de décoration : émaillage, peinture, dorure.";

    protected override Task<PagedResult<DecorationDto>> LireAsync()
        => _service.ListerAsync(new FiltreDecorationsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference", ColonneAlignement.Gauche),
        new("Type", "TypeDecorationNom", ColonneAlignement.Gauche),
        new("Production", "ProductionNumero", ColonneAlignement.Gauche),
        new("Commande", "CommandeNumero", ColonneAlignement.Gauche),
        new("Quantité", "Quantite", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Responsable", "EmployeNom", ColonneAlignement.Gauche),
        new("Coût", "Cout", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
