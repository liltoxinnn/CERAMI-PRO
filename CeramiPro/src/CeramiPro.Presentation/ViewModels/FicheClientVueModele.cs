using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Fiche d'un client : ses coordonnées, ce qu'il a acheté, ce qu'il a
/// commandé, ce qu'il a payé, et les notes prises au fil des échanges.
/// Tout ce qu'il faut avoir sous les yeux quand il rappelle.
/// </summary>
public partial class FicheClientVueModele : VueModeleBase
{
    /// <summary>Au-delà, l'historique se consulte dans les écrans dédiés, avec la recherche.</summary>
    private const int TailleHistorique = 200;

    private readonly IClientService _clients;
    private readonly IVenteService _ventes;
    private readonly ICommandeService _commandes;
    private readonly IPaiementService _paiements;
    private readonly IServiceDialogue _dialogue;

    public FicheClientVueModele(
        ClientDto client, IClientService clients, IVenteService ventes, ICommandeService commandes,
        IPaiementService paiements, IServiceDialogue dialogue)
    {
        _client = client;
        _clients = clients;
        _ventes = ventes;
        _commandes = commandes;
        _paiements = paiements;
        _dialogue = dialogue;
    }

    public override string Titre => $"Fiche client — {Client.Nom}";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Coordonnees), nameof(TotalAchete), nameof(TotalPaye), nameof(Reste),
        nameof(Doit), nameof(Resume))]
    private ClientDto _client;

    /// <summary>Téléphone, Instagram, email et ville, sur une ligne.</summary>
    public string Coordonnees => string.Join("   ·   ",
        new[] { Client.Telephone, Client.Instagram, Client.Email, Client.Ville }
            .Where(v => !string.IsNullOrWhiteSpace(v)));

    public string TotalAchete => Formatage.Montant(Client.TotalDepense);

    public string TotalPaye => Formatage.Montant(Client.TotalPaye);

    public string Reste => Formatage.Montant(Math.Max(0m, Client.Reste));

    public bool Doit => Client.Reste > 0;

    public string Resume => $"Client {Client.Numero} · {Client.NombreVentes} vente(s) · " +
                            $"{Client.NombreCommandes} commande(s)";

    public ObservableCollection<VenteDto> Ventes { get; } = new();

    public ObservableCollection<CommandeDto> Commandes { get; } = new();

    public ObservableCollection<PaiementDto> Paiements { get; } = new();

    public ObservableCollection<NoteClientDto> Notes { get; } = new();

    [ObservableProperty]
    private string _nouvelleNote = string.Empty;

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            Client = await _clients.ObtenirAsync(Client.Id);

            Remplir(Ventes, (await _ventes.ListerAsync(
                new FiltreVentesRequete { ClientId = Client.Id, TaillePage = TailleHistorique })).Elements);

            Remplir(Commandes, (await _commandes.ListerAsync(
                new FiltreCommandesRequete { ClientId = Client.Id, TaillePage = TailleHistorique })).Elements);

            Remplir(Paiements, (await _paiements.ListerAsync(
                new FiltrePaiementsRequete { ClientId = Client.Id, TaillePage = TailleHistorique })).Elements);

            Remplir(Notes, await _clients.ListerNotesAsync(Client.Id));
        });
    }

    /// <summary>Ajoute une note datée : « a appelé pour la couleur du vase ».</summary>
    [RelayCommand]
    private async Task AjouterNoteAsync()
    {
        if (string.IsNullOrWhiteSpace(NouvelleNote))
        {
            MessageErreur = "Écrivez la note avant de l'ajouter.";
            return;
        }

        await ExecuterAsync(async () =>
        {
            await _clients.AjouterNoteAsync(Client.Id, new NoteRequete { Contenu = NouvelleNote.Trim() });
            NouvelleNote = string.Empty;
            Remplir(Notes, await _clients.ListerNotesAsync(Client.Id));
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    private static void Remplir<T>(ObservableCollection<T> cible, IEnumerable<T> source)
    {
        cible.Clear();

        foreach (var element in source)
        {
            cible.Add(element);
        }
    }
}

/// <summary>Construit la fiche d'un client.</summary>
public class FabriqueFicheClient
{
    private readonly IClientService _clients;
    private readonly IVenteService _ventes;
    private readonly ICommandeService _commandes;
    private readonly IPaiementService _paiements;
    private readonly IServiceDialogue _dialogue;

    public FabriqueFicheClient(
        IClientService clients, IVenteService ventes, ICommandeService commandes,
        IPaiementService paiements, IServiceDialogue dialogue)
    {
        _clients = clients;
        _ventes = ventes;
        _commandes = commandes;
        _paiements = paiements;
        _dialogue = dialogue;
    }

    public FicheClientVueModele Pour(ClientDto client)
        => new(client, _clients, _ventes, _commandes, _paiements, _dialogue);
}
