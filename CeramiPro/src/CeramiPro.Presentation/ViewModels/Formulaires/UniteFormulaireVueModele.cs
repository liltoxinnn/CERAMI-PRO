using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Unité de mesure employée par l'atelier : kg, litre, pièce, m²…</summary>
public class UniteFormulaireVueModele : FormulaireVueModele<UniteRequete>
{
    private readonly IUniteService _unites;

    public UniteFormulaireVueModele(IUniteService unites, IServiceLangue langue)
        : base(langue)
        => _unites = unites;

    public override string Titre => EstCreation ? "Nouvelle unité" : "Modifier l'unité";

    public override IReadOnlyList<ChampFormulaire> Champs { get; } = new ChampFormulaire[]
    {
        new("Code", nameof(UniteRequete.Code), TypeChamp.Texte, Obligatoire: true,
            Aide: "Abréviation affichée à côté des quantités : kg, L, pce…"),
        new("Nom", nameof(UniteRequete.Nom), TypeChamp.Texte, Obligatoire: true),
        new("Nature", nameof(UniteRequete.Type), TypeChamp.Liste, Obligatoire: true, Options: Natures),
        new("Facteur de conversion", nameof(UniteRequete.FacteurConversion), TypeChamp.Nombre,
            Aide: "Par rapport à l'unité de référence de cette nature. 1 pour l'unité de référence."),
        new("Unité active", nameof(UniteRequete.Actif), TypeChamp.Case)
    };

    private static IReadOnlyList<OptionChamp> Natures { get; } =
        EnumExtensions.Libelles<UnitType>()
            .Select(t => new OptionChamp((int)t.Valeur, t.Libelle))
            .ToList();

    public override async Task PreparerModificationAsync(int id)
    {
        var unites = await _unites.ListerAsync();

        if (unites.FirstOrDefault(u => u.Id == id) is not { } unite)
        {
            return;
        }

        Id = id;
        Requete = new UniteRequete
        {
            Code = unite.Code,
            Nom = unite.Nom,
            Type = unite.Type,
            FacteurConversion = unite.FacteurConversion,
            Actif = unite.Actif
        };
    }

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _unites.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _unites.CreerAsync(Requete);
        }
    }
}
