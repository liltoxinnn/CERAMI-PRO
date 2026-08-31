using System.Collections.ObjectModel;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Domain.Common;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Écran des rapports : choix du rapport et de la période, affichage, puis
/// export vers un tableur ou impression.
/// </summary>
public partial class RapportsVueModele : VueModeleBase
{
    private readonly IRapportService _rapports;
    private readonly IExportService _exports;
    private readonly IServiceLangue _langue;
    private readonly IServiceFichier _fichiers;
    private readonly IServiceDialogue _dialogue;

    public RapportsVueModele(
        IRapportService rapports,
        IExportService exports,
        IServiceLangue langue,
        IServiceFichier fichiers,
        IServiceDialogue dialogue)
    {
        _rapports = rapports;
        _exports = exports;
        _langue = langue;
        _fichiers = fichiers;
        _dialogue = dialogue;

        var aujourdhui = DateTime.Today;
        _du = new DateTime(aujourdhui.Year, aujourdhui.Month, 1);
        _au = aujourdhui;
    }

    public override string Titre => _langue["menu.rapports"];

    public override string Introduction =>
        "Choisissez un rapport et une période, puis exportez-le vers un tableur ou imprimez-le.";

    /// <summary>Les douze rapports, avec leur nom en français.</summary>
    public IReadOnlyList<(TypeRapport Valeur, string Libelle)> RapportsDisponibles { get; } =
        EnumExtensions.Libelles<TypeRapport>();

    [ObservableProperty]
    private TypeRapport _typeChoisi = TypeRapport.ChiffreAffaires;

    [ObservableProperty]
    private DateTime _du;

    [ObservableProperty]
    private DateTime _au;

    [ObservableProperty]
    private string? _titreRapport;

    [ObservableProperty]
    private string? _periodeRapport;

    /// <summary>En-têtes du tableau affiché.</summary>
    public ObservableCollection<string> Colonnes { get; } = new();

    /// <summary>Lignes du rapport, telles qu'elles seront exportées.</summary>
    public ObservableCollection<IReadOnlyList<string>> Lignes { get; } = new();

    public ObservableCollection<string> Totaux { get; } = new();

    public bool RapportAffiche => Colonnes.Count > 0;

    public bool AucuneDonnee => RapportAffiche && Lignes.Count == 0;

    [RelayCommand]
    private async Task AfficherAsync()
    {
        await ExecuterAsync(async () =>
        {
            var rapport = await _rapports.GenererAsync(Requete());

            TitreRapport = rapport.Titre;
            PeriodeRapport = rapport.Periode;

            Colonnes.Clear();
            foreach (var colonne in rapport.Colonnes) Colonnes.Add(colonne);

            Lignes.Clear();
            foreach (var ligne in rapport.Lignes) Lignes.Add(ligne);

            Totaux.Clear();
            foreach (var total in rapport.Totaux ?? Array.Empty<string>()) Totaux.Add(total);
        });

        OnPropertyChanged(nameof(RapportAffiche));
        OnPropertyChanged(nameof(AucuneDonnee));
    }

    [RelayCommand]
    private Task ExporterExcelAsync() => EnregistrerAsync(
        () => _exports.ExcelAsync(Requete()),
        "Classeur Excel (*.xlsx)|*.xlsx");

    [RelayCommand]
    private Task ExporterPdfAsync() => EnregistrerAsync(
        () => _exports.PdfAsync(Requete()),
        "Document PDF (*.pdf)|*.pdf");

    private async Task EnregistrerAsync(
        Func<Task<(string NomFichier, byte[] Contenu)>> production, string filtre)
        => await ExecuterAsync(async () =>
        {
            var (nomFichier, contenu) = await production();

            if (_fichiers.DemanderOuEnregistrer(nomFichier, filtre) is not { } chemin)
            {
                return;
            }

            await File.WriteAllBytesAsync(chemin, contenu);

            // Ouvrir le fichier tout de suite évite d'aller le chercher dans
            // l'explorateur : c'est presque toujours ce que l'on veut faire.
            if (_dialogue.Confirmer(
                    $"Le fichier a été enregistré :\n{chemin}\n\nVoulez-vous l'ouvrir maintenant ?",
                    "Export terminé"))
            {
                _fichiers.Ouvrir(chemin);
            }
        });

    private RapportRequete Requete() => new()
    {
        Type = TypeChoisi,
        Du = Du,
        Au = Au
    };
}
