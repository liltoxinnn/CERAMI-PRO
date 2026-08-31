using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Ventes au comptoir et sur commande. Une vente se saisit depuis la caisse.</summary>
public partial class VentesVueModele : ListeVueModele<VenteDto>
{
    private readonly IVenteService _service;
    private readonly IDocumentService _documents;

    public VentesVueModele(
        IVenteService service,
        IDocumentService documents,
        IServiceLangue langue,
        OutilsListe outils)
        : base(langue, outils)
    {
        _service = service;
        _documents = documents;
    }


    /// <summary>
    /// Une vente ne se modifie pas : on la réimprime, ou on l'annule, ce qui
    /// remet les produits en stock et laisse une trace.
    /// </summary>
    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Réimprimer le reçu", ImprimerRecuCommand),
        new("Annuler la vente", AnnulerCommand, Destructive: true,
            Aide: "Les produits vendus retournent en stock.")
    };

    [RelayCommand]
    private Task ImprimerRecuAsync() => ImprimerAsync(
        vente => _documents.RecuPdfAsync(vente.Id),
        vente => $"recu-{vente.Numero}.pdf");

    [RelayCommand]
    private Task AnnulerAsync() => AgirAsync(
        vente => _service.AnnulerAsync(vente.Id, "Annulée depuis l'écran des ventes."),
        confirmation: "Annuler cette vente ?\n\n"
                      + "Les produits vendus retourneront en stock et la facture sera annulée.",
        succes: "La vente est annulée.");

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
