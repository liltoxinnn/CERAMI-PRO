using CeramiPro.Application.DTOs.Production;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>Fours de l'atelier : capacité, plage de température et disponibilité.</summary>
public class FoursVueModele : ListeSimpleVueModele<FourDto>
{
    private readonly IFourService _service;

    public FoursVueModele(IFourService service, IServiceLangue langue, OutilsListe outils)
        : base(langue, outils)
        => _service = service;

    protected override Type TypeFormulaire => typeof(FourFormulaireVueModele);

    public override bool PeutSupprimer => true;

    public override string Titre => Langue["menu.cuisson.fours"];

    public override string Introduction =>
        "Fours de l'atelier : capacité, plage de température et disponibilité. " +
        "Un four occupé par une cuisson en cours ne peut pas être réutilisé.";

    protected override Task<IReadOnlyList<FourDto>> LireToutesAsync() => _service.ListerAsync();

    protected override Task SupprimerElementAsync(int id) => _service.SupprimerAsync(id);

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Référence", "Reference"),
        new("Nom", "Nom"),
        new("Capacité", "Capacite", ColonneAlignement.Droite, FormatColonne.Quantite),
        new("Température min.", "TemperatureMin", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Température max.", "TemperatureMax", ColonneAlignement.Droite, FormatColonne.Nombre),
        new("Emplacement", "Emplacement"),
        new("État", "StatutLibelle"),
        new("Cuissons en cours", "CuissonsEnCours", ColonneAlignement.Droite, FormatColonne.Nombre)
    };
}
