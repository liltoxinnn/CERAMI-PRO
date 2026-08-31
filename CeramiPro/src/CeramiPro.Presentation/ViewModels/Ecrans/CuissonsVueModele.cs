using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Enums;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fournées : four employé, température, durée et coût de l'énergie.</summary>
public partial class CuissonsVueModele : ListeVueModele<CuissonDto>
{
    private readonly ICuissonService _service;

    public CuissonsVueModele(ICuissonService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;


    /// <summary>
    /// Le parcours d'une fournée : enfournée, démarrée, puis défournée. Au
    /// défournement, le coût de l'énergie se répartit entre les pièces et le
    /// four redevient disponible.
    /// </summary>
    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Démarrer la cuisson", DemarrerCommand,
            Aide: "Le four passe en service."),
        new("Défourner", DefournerCommand,
            Aide: "Les pièces intactes entrent en stock, les cassées sont enregistrées."),
        new("Annuler la fournée", AnnulerCommand, Destructive: true)
    };

    [RelayCommand]
    private Task DemarrerAsync() => AgirAsync(
        cuisson => _service.DemarrerAsync(cuisson.Id),
        confirmation: "Démarrer cette cuisson ?\n\nLe four passera en service.",
        succes: "La cuisson a démarré.");

    /// <summary>
    /// Défourne en considérant que toutes les pièces sont sorties intactes.
    /// La casse se saisit ensuite au contrôle qualité, qui la trace pièce
    /// par pièce ; c'est là qu'elle doit être décrite.
    /// </summary>
    [RelayCommand]
    private Task DefournerAsync() => AgirAsync(
        async cuisson =>
        {
            var complete = await _service.ObtenirAsync(cuisson.Id);

            await _service.DefournerAsync(cuisson.Id, new DefournementRequete
            {
                Pieces = complete.Pieces.Select(p => new ResultatPieceRequete
                {
                    PieceId = p.Id,
                    QuantiteAcceptee = p.Quantite,
                    QuantiteEndommagee = 0m
                }).ToList()
            });
        },
        confirmation: "Défourner cette cuisson ?\n\n"
                      + "Les pièces seront enregistrées comme sorties intactes ; "
                      + "la casse se saisit au contrôle qualité.",
        succes: "La fournée est défournée et le four est libéré.");

    [RelayCommand]
    private Task AnnulerAsync() => AgirAsync(
        cuisson => _service.AnnulerAsync(cuisson.Id, "Annulée depuis l'écran des cuissons."),
        confirmation: "Annuler cette fournée ?",
        succes: "La fournée est annulée et le four est libéré.");

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
