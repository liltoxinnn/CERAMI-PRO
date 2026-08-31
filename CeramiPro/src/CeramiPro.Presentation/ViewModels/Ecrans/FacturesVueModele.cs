using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.Input;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Factures émises pour les ventes et les commandes personnalisées.</summary>
public partial class FacturesVueModele : ListeVueModele<FactureDto>
{
    private readonly IFactureService _service;
    private readonly IDocumentService _documents;

    public FacturesVueModele(
        IFactureService service,
        IDocumentService documents,
        IServiceLangue langue,
        OutilsListe outils)
        : base(langue, outils)
    {
        _service = service;
        _documents = documents;
    }


    /// <summary>La facture s'imprime au format A4, prête à être remise au client.</summary>
    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("Imprimer la facture", ImprimerCommand)
    };

    [RelayCommand]
    private Task ImprimerAsync() => ImprimerAsync(
        facture => _documents.FacturePdfAsync(facture.Id),
        facture => $"facture-{facture.Numero}.pdf");

    public override string Titre => Langue["menu.factures"];

    public override string Introduction => "Factures émises pour les ventes et les commandes personnalisées.";

    protected override Task<PagedResult<FactureDto>> LireAsync()
        => _service.ListerAsync(new FiltreFacturesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim()
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Émise le", "DateEmission", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Échéance", "DateEcheance", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Payé", "Paye", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Statut", "StatutLibelle", ColonneAlignement.Gauche)
    };
}
