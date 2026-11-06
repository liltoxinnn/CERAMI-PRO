using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fiches clients : coordonnées, historique d'achats et de commandes, reste à payer.</summary>
public partial class ClientsVueModele : ListeVueModele<ClientDto>
{
    private readonly IClientService _service;
    private readonly FabriqueFicheClient _fiches;

    public ClientsVueModele(IClientService service, FabriqueFicheClient fiches, OutilsListe outils)
        : base(outils)
    {
        _service = service;
        _fiches = fiches;

        Filtres = new FiltreListe[]
        {
            new FiltreCase("Seulement les clients qui doivent de l'argent", FiltreChangeAsync)
        };
    }

    protected override Type TypeFormulaire => typeof(ClientFormulaireVueModele);

    /// <summary>
    /// Un client qui a acheté ou commandé ne se supprime pas — le service le
    /// refuse : son historique doit rester lisible. On le désactive.
    /// </summary>
    public override bool PeutSupprimer => true;

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    public override IReadOnlyList<FiltreListe> Filtres { get; }

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("👤  Fiche du client", FicheCommand,
            Aide: "Achats, commandes, paiements et notes de ce client.")
    };

    [RelayCommand]
    private Task FicheAsync() => OuvrirFenetreAsync(_fiches.Pour);

    public override string Titre => "Clients";

    public override string Introduction =>
        "Coordonnées, achats et commandes de chaque client, et ce qu'il reste à payer. " +
        "Ouvrez la fiche d'un client pour voir tout son historique.";

    protected override Task<PagedResult<ClientDto>> LireAsync()
        => _service.ListerAsync(new FiltreClientsRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            SeulementAvecDette = ((FiltreCase)Filtres[0]).Actif
        });

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Nom", "Nom", ColonneAlignement.Gauche),
        new("Téléphone", "Telephone", ColonneAlignement.Gauche),
        new("Instagram", "Instagram", ColonneAlignement.Gauche),
        new("Ville", "Ville", ColonneAlignement.Gauche),
        new("Ventes", "NombreVentes", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Commandes", "NombreCommandes", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Total acheté", "TotalDepense", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste dû", "Reste", ColonneAlignement.Droite, FormatColonne.Montant)
    };
}
