using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Commande personnalisée : une pièce sur mesure demandée par un client.</summary>
public class CommandeFormulaireVueModele : FormulaireVueModele<CommandeRequete>
{
    private readonly ICommandeService _commandes;
    private readonly IClientService _clients;

    public CommandeFormulaireVueModele(
        ICommandeService commandes, IClientService clients, IServiceLangue langue)
        : base(langue)
    {
        _commandes = commandes;
        _clients = clients;
        _champs = Construire(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouvelle commande" : "Modifier la commande";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var clients = await _clients.ListerAsync(new FiltreClientsRequete { TaillePage = 200 });

        _champs = Construire(clients.Elements.Select(c => new OptionChamp(c.Id, c.Nom)).ToList());
        OnPropertyChanged(nameof(Champs));
    }

    public override async Task PreparerModificationAsync(int id)
    {
        var commande = await _commandes.ObtenirAsync(id);

        Id = id;
        Requete = new CommandeRequete
        {
            ClientId = commande.ClientId,
            Titre = commande.Titre,
            Description = commande.Description,
            Largeur = commande.Largeur,
            Hauteur = commande.Hauteur,
            Profondeur = commande.Profondeur,
            Couleurs = commande.Couleurs,
            Materiaux = commande.Materiaux,
            Quantite = commande.Quantite,
            PrixUnitaire = commande.PrixUnitaire,
            Remise = commande.Remise,
            DateLimite = commande.DateLimite,
            EmployeId = commande.EmployeId,
            Notes = commande.Notes
        };
    }

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> clients)
        => new ChampFormulaire[]
        {
            new("Client", nameof(CommandeRequete.ClientId), TypeChamp.Liste,
                Obligatoire: true, Options: clients),
            new("Titre de la commande", nameof(CommandeRequete.Titre), TypeChamp.Texte,
                Obligatoire: true, Aide: "Par exemple : « Service à thé bleu, 12 pièces »."),
            new("Quantité", nameof(CommandeRequete.Quantite), TypeChamp.Nombre, Obligatoire: true),
            new("Prix unitaire", nameof(CommandeRequete.PrixUnitaire), TypeChamp.Montant, Obligatoire: true),
            new("Remise", nameof(CommandeRequete.Remise), TypeChamp.Montant),
            new("Date limite", nameof(CommandeRequete.DateLimite), TypeChamp.Date, Obligatoire: true),
            new("Description", nameof(CommandeRequete.Description), TypeChamp.TexteLong),
            new("Couleurs demandées", nameof(CommandeRequete.Couleurs)),
            new("Matériaux", nameof(CommandeRequete.Materiaux)),
            new("Largeur (cm)", nameof(CommandeRequete.Largeur), TypeChamp.Nombre),
            new("Hauteur (cm)", nameof(CommandeRequete.Hauteur), TypeChamp.Nombre),
            new("Profondeur (cm)", nameof(CommandeRequete.Profondeur), TypeChamp.Nombre),
            new("Notes", nameof(CommandeRequete.Notes), TypeChamp.TexteLong)
        };

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _commandes.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _commandes.CreerAsync(Requete);
        }
    }
}
