using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Four de l'atelier : capacité, plage de température et état.</summary>
public class FourFormulaireVueModele : FormulaireVueModele<FourRequete>
{
    private readonly IFourService _fours;

    public FourFormulaireVueModele(IFourService fours, IServiceLangue langue)
        : base(langue)
        => _fours = fours;

    public override string Titre => EstCreation ? "Nouveau four" : "Modifier le four";

    public override IReadOnlyList<ChampFormulaire> Champs { get; } = new ChampFormulaire[]
    {
        new("Nom", nameof(FourRequete.Nom), TypeChamp.Texte, Obligatoire: true),
        new("Capacité (pièces)", nameof(FourRequete.Capacite), TypeChamp.Nombre, Obligatoire: true),
        new("Température minimale (°C)", nameof(FourRequete.TemperatureMin), TypeChamp.Nombre),
        new("Température maximale (°C)", nameof(FourRequete.TemperatureMax), TypeChamp.Nombre),
        new("Emplacement", nameof(FourRequete.Emplacement)),
        new("État", nameof(FourRequete.Statut), TypeChamp.Liste, Options: Etats),
        new("Notes", nameof(FourRequete.Notes), TypeChamp.TexteLong),
        new("Four actif", nameof(FourRequete.Actif), TypeChamp.Case)
    };

    private static IReadOnlyList<OptionChamp> Etats { get; } =
        EnumExtensions.Libelles<KilnStatus>()
            .Select(e => new OptionChamp((int)e.Valeur, e.Libelle))
            .ToList();

    public override async Task PreparerModificationAsync(int id)
    {
        var fours = await _fours.ListerAsync();

        if (fours.FirstOrDefault(f => f.Id == id) is not { } four)
        {
            return;
        }

        Id = id;
        Requete = new FourRequete
        {
            Nom = four.Nom,
            Capacite = four.Capacite,
            TemperatureMin = four.TemperatureMin,
            TemperatureMax = four.TemperatureMax,
            Emplacement = four.Emplacement,
            Statut = four.Statut,
            Notes = four.Notes,
            Actif = four.Actif
        };
    }

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _fours.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _fours.CreerAsync(Requete);
        }
    }
}
