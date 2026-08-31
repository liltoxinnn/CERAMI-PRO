using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Base commune à tous les écrans qui présentent une liste : recherche,
/// pagination, rechargement, sélection, saisie, suppression et export.
///
/// Écrire ce comportement une seule fois évite dix-huit variantes
/// légèrement différentes, et rend la même ergonomie partout.
/// </summary>
public abstract partial class ListeVueModele<TElement> : VueModeleBase
{
    protected readonly IServiceLangue Langue;

    protected ListeVueModele(IServiceLangue langue, OutilsListe? outils = null)
    {
        Langue = langue;
        Outils = outils;
        Langue.LangueChangee += RafraichirTextes;
    }

    /// <summary>Services communs aux listes ; absents sur les écrans en lecture seule.</summary>
    protected OutilsListe? Outils { get; }

    /// <summary>
    /// Vue-modèle du formulaire de saisie de cet écran. Un écran qui en
    /// déclare une gagne automatiquement les boutons « Ajouter » et
    /// « Modifier » ; les autres restent en consultation.
    /// </summary>
    protected virtual Type? TypeFormulaire => null;

    /// <summary>Vrai lorsque l'écran sait créer une fiche.</summary>
    public virtual bool PeutAjouter => TypeFormulaire is not null && Outils is not null;

    /// <summary>Vrai lorsque l'écran sait rouvrir une fiche existante.</summary>
    public virtual bool PeutModifier => PeutAjouter;

    /// <summary>
    /// Vrai lorsque l'écran sait supprimer une fiche. La suppression reste
    /// refusée par le service métier si la fiche est utilisée ailleurs : ce
    /// drapeau ne fait qu'afficher le bouton.
    /// </summary>
    public virtual bool PeutSupprimer => false;

    /// <summary>L'export est proposé dès qu'un écran a des colonnes et des outils.</summary>
    public bool PeutExporter => Outils is not null;

    /// <summary>Éléments affichés dans le tableau.</summary>
    public ObservableCollection<TElement> Elements { get; } = new();

    [ObservableProperty]
    private TElement? _elementSelectionne;

    [ObservableProperty]
    private string _recherche = string.Empty;

    [ObservableProperty]
    private int _page = 1;

    [ObservableProperty]
    private int _nombreTotal;

    /// <summary>Nombre de lignes par page.</summary>
    public int TaillePage { get; set; } = 25;

    public int NombrePages => NombreTotal == 0 ? 1 : (int)Math.Ceiling((double)NombreTotal / TaillePage);

    public bool PagePrecedenteDisponible => Page > 1;

    public bool PageSuivanteDisponible => Page < NombrePages;

    /// <summary>Vrai quand la recherche n'a rien donné, pour afficher un message.</summary>
    public bool AucunResultat => !ChargementEnCours && Elements.Count == 0;

    public string LibelleRechercher => Langue["action.rechercher"];
    public string LibelleActualiser => Langue["action.actualiser"];
    public string LibelleAjouter => Langue["action.ajouter"];
    public string LibelleModifier => Langue["action.modifier"];
    public string LibelleSupprimer => Langue["action.supprimer"];
    public string LibelleExporter => Langue["action.exporter"];
    public string LibelleAucunResultat => Langue["etat.aucunResultat"];

    /// <summary>Colonnes du tableau, déclarées par chaque écran.</summary>
    public abstract IReadOnlyList<ColonneListe> Colonnes { get; }

    /// <summary>Va chercher une page de résultats auprès du service métier.</summary>
    protected abstract Task<PagedResult<TElement>> LireAsync();

    /// <summary>Supprime la fiche choisie ; à redéfinir par les écrans concernés.</summary>
    protected virtual Task SupprimerElementAsync(int id) => Task.CompletedTask;

    public override Task ChargerAsync() => RafraichirAsync();

    // ------------------------------------------------------------- Lecture

    [RelayCommand]
    protected async Task RafraichirAsync()
    {
        await ExecuterAsync(async () =>
        {
            var resultat = await LireAsync();

            Elements.Clear();
            foreach (var element in resultat.Elements)
            {
                Elements.Add(element);
            }

            NombreTotal = resultat.Total;
        });

        OnPropertyChanged(nameof(NombreTotal));
        OnPropertyChanged(nameof(NombrePages));
        OnPropertyChanged(nameof(AucunResultat));
        OnPropertyChanged(nameof(PagePrecedenteDisponible));
        OnPropertyChanged(nameof(PageSuivanteDisponible));
    }

    /// <summary>Relance la recherche depuis la première page.</summary>
    [RelayCommand]
    private async Task ChercherAsync()
    {
        Page = 1;
        await RafraichirAsync();
    }

    [RelayCommand]
    private async Task PagePrecedenteAsync()
    {
        if (!PagePrecedenteDisponible) return;

        Page--;
        await RafraichirAsync();
    }

    [RelayCommand]
    private async Task PageSuivanteAsync()
    {
        if (!PageSuivanteDisponible) return;

        Page++;
        await RafraichirAsync();
    }

    // -------------------------------------------------------------- Saisie

    /// <summary>Ouvre le formulaire de création, puis recharge la liste.</summary>
    [RelayCommand]
    private Task AjouterAsync() => OuvrirFormulaireAsync(null);

    /// <summary>Rouvre la fiche sélectionnée pour la modifier.</summary>
    [RelayCommand]
    private async Task ModifierAsync()
    {
        if (!PeutModifier)
        {
            return;
        }

        if (IdSelectionne() is not { } id)
        {
            Outils?.Dialogue.Avertissement("Choisissez d'abord une ligne dans le tableau.");
            return;
        }

        await OuvrirFormulaireAsync(id);
    }

    private async Task OuvrirFormulaireAsync(int? id)
    {
        if (Outils is null || TypeFormulaire is null)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            if (Outils.Services.GetService(TypeFormulaire) is not IFormulaire formulaire)
            {
                return;
            }

            // Les listes déroulantes sont chargées avant l'affichage : un
            // formulaire ne doit jamais s'ouvrir sur des choix vides.
            await formulaire.PreparerAsync();

            if (id is { } identifiant)
            {
                await formulaire.PreparerModificationAsync(identifiant);
            }

            if (Outils.Formulaires.Afficher(formulaire))
            {
                await RafraichirAsync();
            }
        });
    }

    /// <summary>Supprime la fiche sélectionnée, après confirmation.</summary>
    [RelayCommand]
    private async Task SupprimerAsync()
    {
        if (!PeutSupprimer || Outils is null)
        {
            return;
        }

        if (IdSelectionne() is not { } id)
        {
            Outils.Dialogue.Avertissement("Choisissez d'abord une ligne dans le tableau.");
            return;
        }

        if (!Outils.Dialogue.Confirmer(Langue["message.confirmerSuppression"]))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            await SupprimerElementAsync(id);
            await RafraichirAsync();
        });

        if (MessageErreur is not null)
        {
            Outils.Dialogue.Erreur(MessageErreur);
        }
    }

    // -------------------------------------------------------------- Export

    [RelayCommand]
    private Task ExporterExcelAsync() => ExporterAsync(
        FormatExport.Excel, "Classeur Excel (*.xlsx)|*.xlsx");

    [RelayCommand]
    private Task ExporterPdfAsync() => ExporterAsync(
        FormatExport.Pdf, "Document PDF (*.pdf)|*.pdf");

    /// <summary>
    /// Exporte la liste entière, et non la seule page affichée : c'est ce
    /// que l'on attend d'un export, et cela évite d'avoir à parcourir les
    /// pages une par une.
    /// </summary>
    private async Task ExporterAsync(FormatExport format, string filtre)
    {
        if (Outils is null)
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            var tout = await LireToutAsync();

            if (tout.Count == 0)
            {
                Outils.Dialogue.Information("Il n'y a rien à exporter pour cette recherche.");
                return;
            }

            var lignes = tout.Select(element => (IReadOnlyList<string>)Colonnes
                .Select(colonne => Texte(element, colonne))
                .ToList()).ToList();

            var (nomFichier, contenu) = await Outils.Exports.TableauAsync(
                Titre, Colonnes.Select(c => c.EnTete).ToList(), lignes, format);

            if (Outils.Fichiers.DemanderOuEnregistrer(nomFichier, filtre) is not { } chemin)
            {
                return;
            }

            await File.WriteAllBytesAsync(chemin, contenu);

            if (Outils.Dialogue.Confirmer(
                    $"Le fichier a été enregistré :\n{chemin}\n\nVoulez-vous l'ouvrir maintenant ?",
                    "Export terminé"))
            {
                Outils.Fichiers.Ouvrir(chemin);
            }
        });
    }

    /// <summary>
    /// Relit la liste entière, page par page : les services plafonnent la
    /// taille d'une page, on la parcourt donc jusqu'au bout.
    ///
    /// La pagination de l'écran est rétablie ensuite, y compris si l'export
    /// échoue : l'utilisateur doit retrouver l'affichage qu'il avait.
    /// </summary>
    private async Task<IReadOnlyList<TElement>> LireToutAsync()
    {
        var taille = TaillePage;
        var page = Page;

        var tout = new List<TElement>();

        try
        {
            TaillePage = TaillePageExport;
            Page = 1;

            while (tout.Count < LimiteExport)
            {
                var resultat = await LireAsync();

                if (resultat.Elements.Count == 0)
                {
                    break;
                }

                tout.AddRange(resultat.Elements);

                if (tout.Count >= resultat.Total)
                {
                    break;
                }

                Page++;
            }
        }
        finally
        {
            TaillePage = taille;
            Page = page;
        }

        return tout;
    }

    /// <summary>Taille des pages lues pendant un export.</summary>
    private const int TaillePageExport = 200;

    /// <summary>
    /// Au-delà, un export devient illisible et pèse trop lourd ; l'atelier a
    /// alors intérêt à filtrer sa recherche.
    /// </summary>
    public const int LimiteExport = 5000;

    /// <summary>Valeur d'une colonne, mise en forme exactement comme à l'écran.</summary>
    private static string Texte(TElement element, ColonneListe colonne)
        => colonne.Formater(element?.GetType().GetProperty(colonne.Propriete)?.GetValue(element));

    // ------------------------------------------------------------ Communs

    /// <summary>
    /// Identifiant de la ligne choisie. Toutes les fiches de l'atelier
    /// portent une propriété « Id » : la lire ici évite de redéfinir la même
    /// méthode dans chacun des dix-huit écrans.
    /// </summary>
    protected int? IdSelectionne()
        => ElementSelectionne?.GetType().GetProperty("Id")?.GetValue(ElementSelectionne) as int?;

    /// <summary>Redemande l'affichage des libellés après un changement de langue.</summary>
    protected virtual void RafraichirTextes()
    {
        OnPropertyChanged(nameof(Titre));
        OnPropertyChanged(nameof(Introduction));
        OnPropertyChanged(nameof(LibelleRechercher));
        OnPropertyChanged(nameof(LibelleActualiser));
        OnPropertyChanged(nameof(LibelleAjouter));
        OnPropertyChanged(nameof(LibelleModifier));
        OnPropertyChanged(nameof(LibelleSupprimer));
        OnPropertyChanged(nameof(LibelleExporter));
        OnPropertyChanged(nameof(LibelleAucunResultat));
    }
}
