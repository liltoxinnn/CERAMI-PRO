using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>
/// Encaissement d'un règlement client.
///
/// Le paiement se rattache à une facture impayée : c'est ainsi que les
/// soldes se mettent à jour tout seuls, sans double saisie.
/// </summary>
public class PaiementFormulaireVueModele : FormulaireVueModele<PaiementRequete>
{
    private readonly IPaiementService _paiements;
    private readonly IFactureService _factures;
    private readonly IReferentielService _referentiels;

    public PaiementFormulaireVueModele(
        IPaiementService paiements,
        IFactureService factures,
        IReferentielService referentiels,
        IServiceLangue langue)
        : base(langue)
    {
        _paiements = paiements;
        _factures = factures;
        _referentiels = referentiels;
        _champs = Construire(Array.Empty<OptionChamp>(), Array.Empty<OptionChamp>());
    }

    private IReadOnlyList<ChampFormulaire> _champs;

    public override string Titre => "Encaisser un règlement";

    public override IReadOnlyList<ChampFormulaire> Champs => _champs;

    public override async Task PreparerAsync()
    {
        var impayees = await _factures.ListerAsync(
            new FiltreFacturesRequete { TaillePage = 200, SeulementImpayees = true });

        var modes = await _referentiels.ListerModesReglementAsync();

        _champs = Construire(
            impayees.Elements
                .Select(f => new OptionChamp(
                    f.Id,
                    $"{f.Numero} — {f.ClientNom} — reste {Application.Common.Formatage.Montant(f.Reste)}"))
                .ToList(),
            modes.Where(m => m.Actif).Select(m => new OptionChamp(m.Id, m.Nom)).ToList());

        OnPropertyChanged(nameof(Champs));
    }

    private static IReadOnlyList<ChampFormulaire> Construire(
        IReadOnlyList<OptionChamp> factures, IReadOnlyList<OptionChamp> modes)
        => new ChampFormulaire[]
        {
            new("Facture réglée", nameof(PaiementRequete.FactureId), TypeChamp.Liste,
                Obligatoire: true, Options: factures,
                Aide: factures.Count == 0
                    ? "Aucune facture impayée : il n'y a rien à encaisser."
                    : null),
            new("Montant reçu", nameof(PaiementRequete.Montant), TypeChamp.Montant, Obligatoire: true),
            new("Date", nameof(PaiementRequete.Date), TypeChamp.Date),
            new("Mode de règlement", nameof(PaiementRequete.ModeReglementId), TypeChamp.Liste,
                Obligatoire: true, Options: modes),
            new("Référence", nameof(PaiementRequete.Reference), TypeChamp.Texte,
                Aide: "Numéro de chèque, de virement ou de transaction."),
            new("Acompte", nameof(PaiementRequete.Acompte), TypeChamp.Case),
            new("Notes", nameof(PaiementRequete.Notes), TypeChamp.TexteLong)
        };

    protected override Task EnregistrerAsync() => _paiements.EnregistrerAsync(Requete);
}
