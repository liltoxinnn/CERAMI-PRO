using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Création et modification d'une matière première.</summary>
public class MatiereFormulaireVueModele : FormulaireVueModele<MatiereRequete>
{
    private readonly IMatiereService _matieres;
    private readonly IReferentielService _referentiels;
    private readonly IUniteService _unites;
    private readonly IFournisseurService _fournisseurs;

    public MatiereFormulaireVueModele(
        IMatiereService matieres,
        IReferentielService referentiels,
        IUniteService unites,
        IFournisseurService fournisseurs,
        IServiceLangue langue)
        : base(langue)
    {
        _matieres = matieres;
        _referentiels = referentiels;
        _unites = unites;
        _fournisseurs = fournisseurs;

        _champs = Construire(Vide, Vide, Vide);
    }

    private static IReadOnlyList<OptionChamp> Vide => Array.Empty<OptionChamp>();

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => EstCreation ? "Nouvelle matière première" : "Modifier la matière";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var categories = await _referentiels.ListerAsync(TypeReferentiel.CategorieMatiere, false);
        var unites = await _unites.ListerAsync(false);
        var fournisseurs = await _fournisseurs.ListerAsync(
            new FiltreFournisseursRequete { TaillePage = 200, InclureInactifs = false });

        _champs = Construire(
            categories.Select(c => new OptionChamp(c.Id, c.Nom)).ToList(),
            unites.Select(u => new OptionChamp(u.Id, $"{u.Nom} ({u.Code})")).ToList(),
            fournisseurs.Elements.Select(f => new OptionChamp(f.Id, f.Nom)).ToList());

        OnPropertyChanged(nameof(Champs));
    }

    public override async Task PreparerModificationAsync(int id)
    {
        var matiere = await _matieres.ObtenirAsync(id);

        Id = id;
        Requete = new MatiereRequete
        {
            Nom = matiere.Nom,
            CategorieId = matiere.CategorieId,
            UniteId = matiere.UniteId,
            StockMinimum = matiere.StockMinimum,
            StockMaximum = matiere.StockMaximum,
            PrixAchat = matiere.PrixDernierAchat,
            FournisseurId = matiere.FournisseurId,
            Emplacement = matiere.Emplacement,
            Description = matiere.Description,
            Image = matiere.Image,
            Actif = matiere.Actif
        };

        // Le stock ne se corrige pas dans cette fiche : il se règle par un
        // mouvement d'inventaire, pour rester traçable.
        _champs = _champs.Where(c => c.Propriete != nameof(MatiereRequete.StockInitial)).ToList();
        OnPropertyChanged(nameof(Champs));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(
        IReadOnlyList<OptionChamp> categories,
        IReadOnlyList<OptionChamp> unites,
        IReadOnlyList<OptionChamp> fournisseurs)
        => new ChampFormulaire[]
        {
            new("Nom", nameof(MatiereRequete.Nom), TypeChamp.Texte, Obligatoire: true),
            new("Catégorie", nameof(MatiereRequete.CategorieId), TypeChamp.Liste,
                Obligatoire: true, Options: categories),
            new("Unité de mesure", nameof(MatiereRequete.UniteId), TypeChamp.Liste,
                Obligatoire: true, Options: unites),
            new("Fournisseur habituel", nameof(MatiereRequete.FournisseurId), TypeChamp.Liste,
                Options: fournisseurs),
            new("Prix d'achat", nameof(MatiereRequete.PrixAchat), TypeChamp.Montant,
                Aide: "Prix payé pour une unité, hors frais de livraison."),
            new("Stock initial", nameof(MatiereRequete.StockInitial), TypeChamp.Nombre,
                Aide: "Quantité déjà présente dans l'atelier. Un mouvement d'entrée sera enregistré."),
            new("Seuil d'alerte", nameof(MatiereRequete.StockMinimum), TypeChamp.Nombre,
                Aide: "En dessous de cette quantité, une alerte est levée."),
            new("Stock maximum", nameof(MatiereRequete.StockMaximum), TypeChamp.Nombre),
            new("Emplacement", nameof(MatiereRequete.Emplacement), TypeChamp.Texte,
                Aide: "Étagère, bac, réserve…"),
            new("Description", nameof(MatiereRequete.Description), TypeChamp.TexteLong),
            new("Matière active", nameof(MatiereRequete.Actif), TypeChamp.Case)
        };

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _matieres.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _matieres.CreerAsync(Requete);
        }
    }
}
