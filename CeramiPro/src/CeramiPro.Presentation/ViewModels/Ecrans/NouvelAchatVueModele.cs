using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Saisie d'un achat de matières premières.
///
/// L'achat est d'abord enregistré en brouillon : les matières n'entrent en
/// stock qu'à la réception, quand on a vérifié ce qui a réellement été livré.
/// </summary>
public partial class NouvelAchatVueModele : DocumentLignesVueModele<AchatRequete>
{
    private readonly IAchatService _achats;
    private readonly IMatiereService _matieres;
    private readonly IFournisseurService _fournisseurs;

    public NouvelAchatVueModele(
        IAchatService achats,
        IMatiereService matieres,
        IFournisseurService fournisseurs,
        IServiceLangue langue,
        IServiceDialogue dialogue)
        : base(langue, dialogue)
    {
        _achats = achats;
        _matieres = matieres;
        _fournisseurs = fournisseurs;

        _champs = Construire(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    /// <summary>Unité et dernier prix d'achat de chaque matière.</summary>
    private readonly Dictionary<int, (int UniteId, decimal Prix, string Unite)> _matiere = new();

    public override string Titre => "Nouvel achat";

    public override string Introduction =>
        "Commande passée à un fournisseur. Les matières n'entrent en stock qu'à la réception, " +
        "depuis l'écran des achats.";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override string NomArticle => "matière";

    public override string LibelleEnregistrer => "Enregistrer l'achat";

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var matieres = await _matieres.ListerAsync(new FiltreMatieresRequete
            {
                TaillePage = 200,
                InclureInactives = false
            });

            Articles.Clear();
            _matiere.Clear();

            foreach (var matiere in matieres.Elements)
            {
                Articles.Add(new OptionChamp(matiere.Id, $"{matiere.Reference} — {matiere.Nom}"));
                _matiere[matiere.Id] = (matiere.UniteId, matiere.PrixDernierAchat, matiere.UniteCode);
            }

            var fournisseurs = await _fournisseurs.ListerAsync(new FiltreFournisseursRequete
            {
                TaillePage = 200,
                InclureInactifs = false
            });

            _champs = Construire(fournisseurs.Elements
                .Select(f => new OptionChamp(f.Id, f.Nom))
                .ToList());

            OnPropertyChanged(nameof(Champs));
        });
    }

    /// <summary>Propose le dernier prix payé pour cette matière.</summary>
    protected override Task ArticleChoisiAsync(int? articleId)
    {
        if (articleId is { } id && _matiere.TryGetValue(id, out var details))
        {
            PrixUnitaire = details.Prix;
        }

        return Task.CompletedTask;
    }

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> fournisseurs)
        => new ChampFormulaire[]
        {
            new("Fournisseur", nameof(AchatRequete.FournisseurId), TypeChamp.Liste,
                Obligatoire: true, Options: fournisseurs),
            new("Date", nameof(AchatRequete.Date), TypeChamp.Date),
            new("Frais de livraison", nameof(AchatRequete.FraisLivraison), TypeChamp.Montant),
            new("Référence de la facture", nameof(AchatRequete.ReferenceFacture), TypeChamp.Texte,
                Aide: "Numéro du bon de livraison ou de la facture du fournisseur."),
            new("Notes", nameof(AchatRequete.Notes), TypeChamp.TexteLong)
        };

    protected override string? Verifier()
        => Requete.FournisseurId == 0 ? "Choisissez le fournisseur de cet achat." : null;

    protected override async Task ValiderAsync()
    {
        Requete.Remise = RemiseDocument;
        Requete.Lignes = Lignes.Select(l => new LigneAchatRequete
        {
            MatiereId = l.ArticleId,
            UniteId = _matiere.TryGetValue(l.ArticleId, out var details) ? details.UniteId : 0,
            Quantite = l.Quantite,
            PrixUnitaire = l.PrixUnitaire,
            Remise = l.Remise
        }).ToList();

        var achat = await _achats.CreerAsync(Requete);

        Reinitialiser();

        Dialogue.Succes(
            $"L'achat {achat.Numero} a été enregistré en brouillon.\n\n" +
            "Confirmez-le puis enregistrez sa réception depuis l'écran des achats : " +
            "c'est à ce moment-là que les matières entreront en stock.");
    }
}
