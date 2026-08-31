using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Caisse de l'atelier : vente au comptoir.
///
/// L'écran est conçu pour aller vite : on scanne ou on choisit un produit,
/// la quantité vaut un par défaut, le rendu de monnaie s'affiche pendant
/// que l'on tape le montant reçu, et le reçu s'imprime à la validation.
/// </summary>
public partial class CaisseVueModele : DocumentLignesVueModele<VenteRequete>
{
    private readonly IVenteService _ventes;
    private readonly IProduitService _produits;
    private readonly IClientService _clients;
    private readonly IReferentielService _referentiels;
    private readonly IDocumentService _documents;
    private readonly IServiceFichier _fichiers;

    public CaisseVueModele(
        IVenteService ventes,
        IProduitService produits,
        IClientService clients,
        IReferentielService referentiels,
        IDocumentService documents,
        IServiceFichier fichiers,
        IServiceLangue langue,
        IServiceDialogue dialogue)
        : base(langue, dialogue)
    {
        _ventes = ventes;
        _produits = produits;
        _clients = clients;
        _referentiels = referentiels;
        _documents = documents;
        _fichiers = fichiers;

        _champs = Construire(Array.Empty<OptionChamp>(), Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => Langue["menu.caisse"];

    public override string Introduction =>
        "Vente au comptoir. Scannez l'étiquette du produit ou choisissez-le dans la liste, " +
        "puis encaissez : le stock, la facture et le reçu suivent tout seuls.";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override string NomArticle => "produit";

    public override string LibelleEnregistrer => "Encaisser";

    public override bool GereReglement => true;

    public override bool AccepteScan => true;

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var produits = await _produits.ListerAsync(new FiltreProduitsRequete
            {
                TaillePage = 200,
                InclureInactifs = false
            });

            Articles.Clear();
            foreach (var produit in produits.Elements)
            {
                Articles.Add(new OptionChamp(produit.Id, $"{produit.Reference} — {produit.Nom}"));
                _prix[produit.Id] = produit.PrixVente;
            }

            var clients = await _clients.ListerAsync(new FiltreClientsRequete { TaillePage = 200 });
            var modes = await _referentiels.ListerModesReglementAsync();

            _champs = Construire(
                clients.Elements.Select(c => new OptionChamp(c.Id, c.Nom)).ToList(),
                modes.Where(m => m.Actif).Select(m => new OptionChamp(m.Id, m.Nom)).ToList());

            OnPropertyChanged(nameof(Champs));
        });
    }

    /// <summary>Prix de vente de chaque produit, pour ne pas le redemander à la base.</summary>
    private readonly Dictionary<int, decimal> _prix = new();

    /// <summary>Propose le prix du catalogue dès que le produit est choisi.</summary>
    protected override Task ArticleChoisiAsync(int? articleId)
    {
        if (articleId is { } id && _prix.TryGetValue(id, out var prix))
        {
            PrixUnitaire = prix;
        }

        return Task.CompletedTask;
    }

    /// <summary>
    /// Ajoute le produit dont l'étiquette vient d'être scannée. Un code
    /// inconnu ne bloque pas la caisse : il affiche un message et la vente
    /// continue.
    /// </summary>
    protected override async Task ScannerAsync(string code)
    {
        var produit = await _produits.RechercherParCodeAsync(code);

        if (produit is null)
        {
            MessageErreur = $"Aucun produit ne correspond au code « {code} ».";
            return;
        }

        _prix[produit.Id] = produit.PrixVente;

        Ajouter(produit.Id, $"{produit.Reference} — {produit.Nom}", 1m, produit.PrixVente,
            reference: produit.Reference);
    }

    private static IReadOnlyList<ChampFormulaire> Construire(
        IReadOnlyList<OptionChamp> clients, IReadOnlyList<OptionChamp> modes)
        => new ChampFormulaire[]
        {
            new("Client", nameof(VenteRequete.ClientId), TypeChamp.Liste, Options: clients,
                Aide: "Laissez vide pour une vente au comptoir sans client identifié."),
            new("Mode de règlement", nameof(VenteRequete.ModeReglementId), TypeChamp.Liste,
                Options: modes),
            new("Notes", nameof(VenteRequete.Notes), TypeChamp.TexteLong)
        };

    protected override string? Verifier()
    {
        if (MontantPaye < 0m)
        {
            return "Le montant reçu ne peut pas être négatif.";
        }

        if (Reste > 0m && Requete.ClientId is null)
        {
            return "Une vente réglée en partie doit être rattachée à un client, " +
                   "sans quoi le reste dû ne pourrait être réclamé à personne.";
        }

        return null;
    }

    protected override async Task ValiderAsync()
    {
        Requete.Remise = RemiseDocument;
        Requete.MontantPaye = MontantPaye;
        Requete.EmettreFacture = true;
        Requete.Lignes = Lignes.Select(l => new LigneVenteRequete
        {
            ProduitId = l.ArticleId,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Remise = l.Remise
        }).ToList();

        var vente = await _ventes.EnregistrerAsync(Requete);
        var rendu = RenduAffiche;

        Reinitialiser();

        if (Dialogue.Confirmer(
                $"Vente {vente.Numero} enregistrée.\n\n" +
                $"Total : {Application.Common.Formatage.Montant(vente.Total)}\n" +
                $"Monnaie à rendre : {rendu}\n\n" +
                "Voulez-vous imprimer le reçu ?",
                "Vente enregistrée"))
        {
            await ImprimerRecuAsync(vente);
        }
    }

    /// <summary>Produit le reçu au format d'un rouleau de 80 mm et l'ouvre.</summary>
    private async Task ImprimerRecuAsync(VenteDto vente)
    {
        try
        {
            var contenu = await _documents.RecuPdfAsync(vente.Id);
            var nomFichier = $"recu-{vente.Numero}.pdf";

            if (_fichiers.DemanderOuEnregistrer(nomFichier, "Document PDF (*.pdf)|*.pdf") is not { } chemin)
            {
                return;
            }

            await File.WriteAllBytesAsync(chemin, contenu);
            _fichiers.Ouvrir(chemin);
        }
        catch (Exception)
        {
            // La vente est enregistrée : un souci d'impression ne doit pas
            // laisser croire le contraire.
            Dialogue.Avertissement(
                "La vente est bien enregistrée, mais le reçu n'a pas pu être produit.\n\n" +
                "Vous pouvez le réimprimer depuis l'écran des ventes.");
        }
    }
}
