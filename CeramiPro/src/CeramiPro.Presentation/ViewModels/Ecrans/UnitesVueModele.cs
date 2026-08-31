using CeramiPro.Application.DTOs.Referentiels;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Unités de mesure employées par l'atelier.</summary>
public class UnitesVueModele : ListeSimpleVueModele<UniteDto>
{
    private readonly IUniteService _service;

    public UnitesVueModele(IUniteService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(UniteFormulaireVueModele);

    public override bool PeutSupprimer => true;

    public override string Titre => Langue["menu.administration.unites"];

    public override string Introduction =>
        "Kilogramme, litre, pièce, mètre carré… Une unité déjà employée par une matière ne peut plus être supprimée.";

    protected override Task<IReadOnlyList<UniteDto>> LireToutesAsync() => _service.ListerAsync();

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Code", "Code"),
        new("Nom", "Nom"),
        new("Nature", "TypeLibelle"),
        new("Conversion", "FacteurConversion", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Utilisations", "NombreUtilisations", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Fournie avec le logiciel", "Systeme", ColonneAlignement.Centre),
        new("Active", "Actif", ColonneAlignement.Centre)
    };
}
