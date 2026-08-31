using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Encaissements reçus des clients.</summary>
public partial class PaiementsVueModele : ListeVueModele<PaiementDto>
{
    private readonly IPaiementService _service;

    public PaiementsVueModele(IPaiementService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(PaiementFormulaireVueModele);

    /// <summary>Un paiement se corrige par une annulation tracée, jamais par une modification.</summary>
    public override bool PeutModifier => false;

    public override string Titre => Langue["menu.paiements"];

    public override string Introduction => "Encaissements reçus des clients : acomptes, soldes et règlements de factures.";

    protected override Task<PagedResult<PaiementDto>> LireAsync()
        => _service.ListerAsync(new FiltrePaiementsRequete
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
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Vente", "VenteNumero", ColonneAlignement.Gauche),
        new("Facture", "FactureNumero", ColonneAlignement.Gauche),
        new("Montant", "Montant", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Mode", "ModeReglement", ColonneAlignement.Gauche),
        new("Acompte", "Acompte", ColonneAlignement.Centre),
        new("Encaissé par", "Utilisateur", ColonneAlignement.Gauche)
    };
}
