using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Deux calculs que l'atelier refait sans cesse : la surface à couvrir en
/// carrelage, et le nombre de pièces à lancer en tenant compte de la casse.
/// </summary>
public partial class CalculateursVueModele : VueModeleBase
{
    private readonly ICalculateurService _calculateur;
    private readonly IServiceLangue _langue;

    public CalculateursVueModele(ICalculateurService calculateur, IServiceLangue langue)
    {
        _calculateur = calculateur;
        _langue = langue;
    }

    public override string Titre => _langue["menu.calculateurs"];

    public override string Introduction =>
        "Surface à couvrir et quantité à lancer, perte comprise. Rien n'est enregistré : " +
        "ces calculs servent à préparer un devis ou une fabrication.";

    // ------------------------------------------------------------- Surface

    [ObservableProperty]
    private decimal _longueur;

    [ObservableProperty]
    private decimal _largeur;

    [ObservableProperty]
    private int _nombrePieces = 1;

    [ObservableProperty]
    private decimal _pertesurface = 10m;

    [ObservableProperty]
    private string? _surfaceUnitaire;

    [ObservableProperty]
    private string? _surfaceTotale;

    [ObservableProperty]
    private string? _surfacePerte;

    [ObservableProperty]
    private string? _surfaceAvecPerte;

    public bool SurfaceCalculee => SurfaceAvecPerte is not null;

    [RelayCommand]
    private void CalculerSurface()
    {
        MessageErreur = null;

        if (Longueur <= 0 || Largeur <= 0)
        {
            MessageErreur = "Indiquez une longueur et une largeur supérieures à zéro.";
            return;
        }

        var resultat = _calculateur.Surface(new CalculSurfaceRequete
        {
            Longueur = Longueur,
            Largeur = Largeur,
            NombrePieces = NombrePieces < 1 ? 1 : NombrePieces,
            PourcentagePerte = Pertesurface
        });

        SurfaceUnitaire = Formatage.Quantite(resultat.SurfaceUnitaire, "m²");
        SurfaceTotale = Formatage.Quantite(resultat.SurfaceTotale, "m²");
        SurfacePerte = Formatage.Quantite(resultat.Perte, "m²");
        SurfaceAvecPerte = Formatage.Quantite(resultat.SurfaceAvecPerte, "m²");

        OnPropertyChanged(nameof(SurfaceCalculee));
    }

    // ------------------------------------------------------------ Quantité

    [ObservableProperty]
    private decimal _quantiteParUnite = 1m;

    [ObservableProperty]
    private decimal _quantiteSouhaitee;

    [ObservableProperty]
    private decimal _perteQuantite = 10m;

    [ObservableProperty]
    private string? _quantiteNecessaire;

    [ObservableProperty]
    private string? _quantiteAvecPerte;

    [ObservableProperty]
    private string? _unitesNecessaires;

    public bool QuantiteCalculee => QuantiteAvecPerte is not null;

    [RelayCommand]
    private void CalculerQuantite()
    {
        MessageErreur = null;

        if (QuantiteSouhaitee <= 0)
        {
            MessageErreur = "Indiquez la quantité souhaitée.";
            return;
        }

        var resultat = _calculateur.Quantite(new CalculQuantiteRequete
        {
            QuantiteParUnite = QuantiteParUnite <= 0 ? 1m : QuantiteParUnite,
            QuantiteSouhaitee = QuantiteSouhaitee,
            PourcentagePerte = PerteQuantite
        });

        QuantiteNecessaire = Formatage.Quantite(resultat.QuantiteNecessaire);
        QuantiteAvecPerte = Formatage.Quantite(resultat.QuantiteAvecPerte);
        UnitesNecessaires = Formatage.Quantite(resultat.UnitesNecessaires);

        OnPropertyChanged(nameof(QuantiteCalculee));
    }
}
