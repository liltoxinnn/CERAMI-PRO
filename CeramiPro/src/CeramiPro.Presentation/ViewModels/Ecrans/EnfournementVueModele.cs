using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Enfournement : composition d'une fournée.
///
/// On choisit le four, le type de cuisson et la température, puis on ajoute
/// les pièces à enfourner. Le coût de l'énergie sera réparti entre elles au
/// défournement, au prorata des quantités.
/// </summary>
public partial class EnfournementVueModele : DocumentLignesVueModele<CuissonRequete>
{
    private readonly ICuissonService _cuissons;
    private readonly IFourService _fours;
    private readonly IProduitService _produits;

    public EnfournementVueModele(
        ICuissonService cuissons,
        IFourService fours,
        IProduitService produits,
        IServiceLangue langue,
        IServiceDialogue dialogue)
        : base(langue, dialogue)
    {
        _cuissons = cuissons;
        _fours = fours;
        _produits = produits;

        _champs = Construire(Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => "Enfourner";

    public override string Introduction =>
        "Composition d'une fournée. Le coût de l'énergie sera réparti entre les pièces " +
        "au défournement, au prorata des quantités.";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override string NomArticle => "produit à cuire";

    public override string LibelleEnregistrer => "Enfourner";

    /// <summary>Les pièces enfournées n'ont pas de prix : elles ne sont pas vendues ici.</summary>
    public override bool AfficherPrix => false;

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
            }

            var fours = await _fours.ListerAsync();

            _champs = Construire(fours
                .Where(f => f.Actif)
                .Select(f => new OptionChamp(
                    f.Id,
                    f.Statut == KilnStatus.Disponible
                        ? f.Nom
                        : $"{f.Nom} ({f.StatutLibelle})"))
                .ToList());

            OnPropertyChanged(nameof(Champs));
        });
    }

    private static IReadOnlyList<ChampFormulaire> Construire(IReadOnlyList<OptionChamp> fours)
        => new ChampFormulaire[]
        {
            new("Four", nameof(CuissonRequete.FourId), TypeChamp.Liste,
                Obligatoire: true, Options: fours),
            new("Type de cuisson", nameof(CuissonRequete.Type), TypeChamp.Liste,
                Obligatoire: true, Options: TypesCuisson),
            new("Température (°C)", nameof(CuissonRequete.Temperature), TypeChamp.Nombre,
                Obligatoire: true),
            new("Début", nameof(CuissonRequete.Debut), TypeChamp.Date),
            new("Coût de l'énergie", nameof(CuissonRequete.CoutEnergie), TypeChamp.Montant,
                Aide: "Gaz ou électricité consommés par la fournée."),
            new("Observations", nameof(CuissonRequete.Observations), TypeChamp.TexteLong)
        };

    private static IReadOnlyList<OptionChamp> TypesCuisson { get; } =
        EnumExtensions.Libelles<FiringType>()
            .Select(t => new OptionChamp((int)t.Valeur, t.Libelle))
            .ToList();

    protected override string? Verifier()
    {
        if (Requete.FourId == 0)
        {
            return "Choisissez le four employé pour cette cuisson.";
        }

        return Requete.Temperature <= 0m
            ? "Indiquez la température de cuisson."
            : null;
    }

    protected override async Task ValiderAsync()
    {
        Requete.Pieces = Lignes.Select(l => new PieceCuissonRequete
        {
            ProduitId = l.ArticleId,
            Quantite = l.Quantite
        }).ToList();

        var cuisson = await _cuissons.CreerAsync(Requete);

        Reinitialiser();

        Dialogue.Succes(
            $"La fournée {cuisson.Numero} a été enregistrée dans le four « {cuisson.FourNom} ».\n\n" +
            "Démarrez-la puis défournez-la depuis l'écran des cuissons.");
    }
}
