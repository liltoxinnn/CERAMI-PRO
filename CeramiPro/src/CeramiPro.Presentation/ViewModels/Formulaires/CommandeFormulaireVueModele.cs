using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Commande personnalisée : une pièce sur mesure demandée par un client, à
/// l'atelier ou par Instagram, WhatsApp… L'acompte s'encaisse ensuite depuis
/// la liste des commandes, et le reste à payer se calcule tout seul.
/// </summary>
public class CommandeFormulaireVueModele : FormulaireVueModele<CommandeRequete>
{
    private readonly ICommandeService _commandes;
    private readonly IClientService _clients;

    public CommandeFormulaireVueModele(ICommandeService commandes, IClientService clients)
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
        var clients = await _clients.ListerPourChoixAsync();

        _champs = Construire(clients.Select(c => new OptionChamp(c.Id, c.Libelle)).ToList());
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
            Canal = commande.Canal,
            Description = commande.Description,
            Largeur = commande.Largeur,
            Hauteur = commande.Hauteur,
            Profondeur = commande.Profondeur,
            Couleurs = commande.Couleurs,
            Materiaux = commande.Materiaux,
            Quantite = commande.Quantite,
            PrixUnitaire = commande.PrixUnitaire,
            Remise = commande.Remise,
            DateLimite = commande.DateLimite.Date,
            EmployeId = commande.EmployeId,
            Notes = commande.Notes
        };
    }

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> clients)
        => new ChampFormulaire[]
        {
            new("Client", nameof(CommandeRequete.ClientId), TypeChamp.Liste,
                Obligatoire: true, Options: clients,
                Aide: clients.Count == 0
                    ? "Aucun client : créez-le d'abord avec « Nouveau client »."
                    : "Tapez le début du nom pour le retrouver."),
            new("Reçue par", nameof(CommandeRequete.Canal), TypeChamp.Liste,
                Obligatoire: true, Options: OptionChamp.De<CanalCommande>()),
            new("Titre de la commande", nameof(CommandeRequete.Titre), TypeChamp.Texte,
                Obligatoire: true, Aide: "Par exemple : « Service à thé bleu, 12 pièces »."),
            new("Quantité", nameof(CommandeRequete.Quantite), TypeChamp.Nombre, Obligatoire: true),
            new("Prix unitaire", nameof(CommandeRequete.PrixUnitaire), TypeChamp.Montant, Obligatoire: true),
            new("Remise", nameof(CommandeRequete.Remise), TypeChamp.Montant,
                Aide: "Total = quantité × prix unitaire − remise. L'acompte s'encaisse ensuite depuis la liste."),
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
