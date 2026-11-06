using System.Collections.ObjectModel;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Écran des rapports : choix du rapport et de la période, affichage avec
/// graphique, puis export vers un tableur ou impression.
/// </summary>
public partial class RapportsVueModele : VueModeleBase
{
    private readonly IRapportService _rapports;
    private readonly IExportService _exports;
    private readonly IServiceFichier _fichiers;
    private readonly IServiceDialogue _dialogue;

    public RapportsVueModele(
        IRapportService rapports,
        IExportService exports,
        IServiceFichier fichiers,
        IServiceDialogue dialogue)
    {
        _rapports = rapports;
        _exports = exports;
        _fichiers = fichiers;
        _dialogue = dialogue;

        var aujourdhui = DateTime.Today;
        _du = new DateTime(aujourdhui.Year, aujourdhui.Month, 1);
        _au = aujourdhui;
    }

    public override string Titre => "Rapports";

    public override string Introduction =>
        "Ventes du jour ou du mois, bénéfices, dépenses, dettes et état du stock. Choisissez un rapport " +
        "et une période ; le tableau s'exporte vers Excel ou s'imprime en PDF.";

    /// <summary>
    /// Les rapports proposés. Des enregistrements et non des tuples : WPF ne
    /// sait lier que des propriétés, et les noms d'un tuple n'existent qu'à
    /// la compilation.
    /// </summary>
    public IReadOnlyList<OptionRapport> RapportsDisponibles { get; } = EnumExtensions.Libelles<TypeRapport>()
        .Select(r => new OptionRapport(r.Valeur, r.Libelle))
        .ToList();

    [ObservableProperty]
    private TypeRapport _typeChoisi = TypeRapport.VentesParJour;

    /// <summary>
    /// Les rapports mois par mois couvrent l'année : sur un seul mois, ils
    /// n'auraient qu'une ligne. Le rapport se met à jour aussitôt.
    /// </summary>
    partial void OnTypeChoisiChanged(TypeRapport value)
    {
        if (value is TypeRapport.VentesParMois or TypeRapport.Benefices
            && Du.Year == Au.Year && Du.Month == Au.Month)
        {
            Du = new DateTime(Au.Year, 1, 1);
        }

        _ = AfficherAsync();
    }

    /// <summary>Vrai pour les rapports qui ne dépendent pas de la période.</summary>
    public bool SansPeriode => TypeChoisi is TypeRapport.DettesClients or TypeRapport.ValeurStock;

    [ObservableProperty]
    private string? _remarque;

    /// <summary>Graphique du rapport, quand il en a un.</summary>
    public ObservableCollection<BarreGraphique> Barres { get; } = new();

    public bool AGraphique => Barres.Count > 0;

    [ObservableProperty]
    private string? _maximumGraphique;

    public override Task ChargerAsync() => AfficherAsync();

    // ------------------------------------------------------- Périodes rapides

    [RelayCommand]
    private Task AujourdhuiAsync() => PeriodeAsync(DateTime.Today, DateTime.Today);

    [RelayCommand]
    private Task CeMoisAsync()
        => PeriodeAsync(new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1), DateTime.Today);

    [RelayCommand]
    private Task MoisDernierAsync()
    {
        var debut = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1).AddMonths(-1);
        return PeriodeAsync(debut, debut.AddMonths(1).AddDays(-1));
    }

    [RelayCommand]
    private Task CetteAnneeAsync() => PeriodeAsync(new DateTime(DateTime.Today.Year, 1, 1), DateTime.Today);

    private Task PeriodeAsync(DateTime du, DateTime au)
    {
        Du = du;
        Au = au;
        return AfficherAsync();
    }

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
            Remarque = rapport.Remarque;

            var points = rapport.Graphique ?? Array.Empty<PointGraphiqueDto>();
            MaximumGraphique = Graphique.Remplir(Barres, points, Graphique.PasDesEtiquettes(points.Count));

            Colonnes.Clear();
            foreach (var colonne in rapport.Colonnes) Colonnes.Add(colonne);

            Lignes.Clear();
            foreach (var ligne in rapport.Lignes) Lignes.Add(ligne);

            Totaux.Clear();
            foreach (var total in rapport.Totaux ?? Array.Empty<string>()) Totaux.Add(total);
        });

        OnPropertyChanged(nameof(RapportAffiche));
        OnPropertyChanged(nameof(AucuneDonnee));
        OnPropertyChanged(nameof(AGraphique));
        OnPropertyChanged(nameof(SansPeriode));
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

/// <summary>Un rapport proposé dans la liste de choix.</summary>
public record OptionRapport(TypeRapport Valeur, string Libelle);
