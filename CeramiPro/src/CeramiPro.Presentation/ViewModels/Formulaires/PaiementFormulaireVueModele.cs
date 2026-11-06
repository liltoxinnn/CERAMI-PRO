using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Encaissement d'un règlement client : le solde d'une vente à crédit, ou
/// un acompte ou un solde sur une commande.
///
/// Le paiement se rattache toujours à un document qui a un reste à payer :
/// c'est ainsi que les soldes se mettent à jour tout seuls, sans double saisie.
/// </summary>
public class PaiementFormulaireVueModele : FormulaireVueModele<ReglementRequete>
{
    private readonly IPaiementService _paiements;
    private readonly IVenteService _ventes;
    private readonly ICommandeService _commandes;
    private readonly IReferentielService _referentiels;

    /// <summary>Commandes proposées, pour savoir si le règlement est un acompte.</summary>
    private Dictionary<int, CommandeDto> _commandesOuvertes = new();

    public PaiementFormulaireVueModele(
        IPaiementService paiements,
        IVenteService ventes,
        ICommandeService commandes,
        IReferentielService referentiels)
    {
        _paiements = paiements;
        _ventes = ventes;
        _commandes = commandes;
        _referentiels = referentiels;
        _champs = Construire(Array.Empty<OptionChamp>(), Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => "Encaisser un règlement";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var ventes = await _ventes.ListerAsync(
            new FiltreVentesRequete { TaillePage = 200, SeulementImpayees = true });

        var commandes = await _commandes.ListerAsync(
            new FiltreCommandesRequete { TaillePage = 200, SeulementAvecReste = true });

        var modes = await _referentiels.ListerModesReglementAsync();

        _commandesOuvertes = commandes.Elements.ToDictionary(c => c.Id);

        var documents = ventes.Elements
            .Select(v => new OptionChamp(
                ReglementRequete.CodeVente(v.Id),
                $"Vente {v.Numero} — {v.ClientNom} — reste {Formatage.Montant(v.Reste)}"))
            .Concat(commandes.Elements.Select(c => new OptionChamp(
                ReglementRequete.CodeCommande(c.Id),
                $"Commande {c.Numero} — {c.ClientNom} — reste {Formatage.Montant(c.Reste)}")))
            .ToList();

        _champs = Construire(
            documents,
            modes.Where(m => m.Actif).Select(m => new OptionChamp(m.Id, m.Nom)).ToList());

        // Le mode le plus courant est proposé d'office.
        if (Requete.ModeReglementId == 0 && modes.FirstOrDefault(m => m.Actif) is { } premier)
        {
            Requete.ModeReglementId = premier.Id;
        }

        OnPropertyChanged(nameof(Champs));
    }

    /// <summary>
    /// Ouvre le formulaire sur une vente ou une commande précise, avec son
    /// reste à payer proposé comme montant.
    /// </summary>
    public void Preselectionner(int code, decimal reste)
    {
        Requete.Document = code;
        Requete.Montant = reste;
        OnPropertyChanged(nameof(Requete));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(
        IReadOnlyList<OptionChamp> documents, IReadOnlyList<OptionChamp> modes)
        => new ChampFormulaire[]
        {
            new("Règlement de", nameof(ReglementRequete.Document), TypeChamp.Liste,
                Obligatoire: true, Options: documents,
                Aide: documents.Count == 0
                    ? "Aucune vente ni commande n'a de reste à payer : il n'y a rien à encaisser."
                    : "Ventes à crédit et commandes dont il reste une somme à encaisser."),
            new("Montant reçu", nameof(ReglementRequete.Montant), TypeChamp.Montant, Obligatoire: true),
            new("Date", nameof(ReglementRequete.Date), TypeChamp.Date),
            new("Mode de règlement", nameof(ReglementRequete.ModeReglementId), TypeChamp.Liste,
                Obligatoire: true, Options: modes),
            new("Référence", nameof(ReglementRequete.Reference), TypeChamp.Texte,
                Aide: "Numéro de chèque, de virement ou de transaction."),
            new("Notes", nameof(ReglementRequete.Notes), TypeChamp.TexteLong)
        };

    protected override Task EnregistrerAsync()
    {
        var requete = new PaiementRequete
        {
            Montant = Requete.Montant,
            Date = Requete.Date,
            ModeReglementId = Requete.ModeReglementId,
            Reference = Requete.Reference,
            Notes = Requete.Notes
        };

        if (ReglementRequete.EstCommande(Requete.Document, out var commandeId))
        {
            requete.CommandeId = commandeId;

            // Tant que la pièce n'est pas livrée, ce qu'on reçoit est un acompte.
            requete.Acompte = _commandesOuvertes.TryGetValue(commandeId, out var commande)
                              && commande.Statut != CustomOrderStatus.Livree;
        }
        else
        {
            requete.VenteId = Requete.Document;
        }

        return _paiements.EnregistrerAsync(requete);
    }
}

/// <summary>
/// Saisie d'un règlement. Ventes et commandes figurent dans la même liste :
/// une commande y est repérée par un code négatif, une vente par son
/// identifiant.
/// </summary>
public class ReglementRequete
{
    public int Document { get; set; }
    public decimal Montant { get; set; }
    public DateTime? Date { get; set; }
    public int ModeReglementId { get; set; }
    public string? Reference { get; set; }
    public string? Notes { get; set; }

    public static int CodeVente(int venteId) => venteId;

    public static int CodeCommande(int commandeId) => -commandeId;

    public static bool EstCommande(int code, out int commandeId)
    {
        commandeId = -code;
        return code < 0;
    }
}
