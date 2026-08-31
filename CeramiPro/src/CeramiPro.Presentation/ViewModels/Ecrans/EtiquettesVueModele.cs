using System.Collections.ObjectModel;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Codes;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Étiquettes des produits : code-barres, code QR et prix, prêts à imprimer
/// et à coller sur les pièces.
///
/// Le code-barres imprimé est celui que lit la douchette de la caisse :
/// c'est ce qui permet d'encaisser sans chercher le produit dans une liste.
/// </summary>
public partial class EtiquettesVueModele : VueModeleBase
{
    private readonly IProduitService _produits;
    private readonly ICodeService _codes;
    private readonly IDocumentService _documents;
    private readonly IServiceLangue _langue;
    private readonly IServiceDialogue _dialogue;
    private readonly IServiceFichier _fichiers;

    public EtiquettesVueModele(
        IProduitService produits,
        ICodeService codes,
        IDocumentService documents,
        IServiceLangue langue,
        IServiceDialogue dialogue,
        IServiceFichier fichiers)
    {
        _produits = produits;
        _codes = codes;
        _documents = documents;
        _langue = langue;
        _dialogue = dialogue;
        _fichiers = fichiers;
    }

    public override string Titre => _langue["menu.produits.etiquettes"];

    public override string Introduction =>
        "Choisissez les produits à étiqueter, puis imprimez la planche. " +
        "Le code-barres imprimé est celui que lit la douchette de la caisse.";

    /// <summary>Produits proposés au choix, avec leur case à cocher.</summary>
    public ObservableCollection<ChoixProduit> Produits { get; } = new();

    /// <summary>Étiquettes construites pour les produits retenus.</summary>
    public ObservableCollection<EtiquetteDto> Etiquettes { get; } = new();

    [ObservableProperty]
    private string _recherche = string.Empty;

    [ObservableProperty]
    private int _exemplaires = 1;

    public bool AucuneEtiquette => !ChargementEnCours && Etiquettes.Count == 0;

    public int NombreChoisis => Produits.Count(p => p.Choisi);

    public override Task ChargerAsync() => ChercherAsync();

    [RelayCommand]
    private async Task ChercherAsync()
    {
        await ExecuterAsync(async () =>
        {
            var resultat = await _produits.ListerAsync(new FiltreProduitsRequete
            {
                TaillePage = 200,
                Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
                InclureInactifs = false
            });

            Produits.Clear();
            foreach (var produit in resultat.Elements)
            {
                Produits.Add(new ChoixProduit(produit, SignalerChoix));
            }
        });
    }

    [RelayCommand]
    private void ToutChoisir()
    {
        foreach (var produit in Produits)
        {
            produit.Choisi = true;
        }

        OnPropertyChanged(nameof(NombreChoisis));
    }

    [RelayCommand]
    private void ToutDecocher()
    {
        foreach (var produit in Produits)
        {
            produit.Choisi = false;
        }

        OnPropertyChanged(nameof(NombreChoisis));
    }

    /// <summary>Construit la planche d'étiquettes des produits cochés.</summary>
    [RelayCommand]
    private async Task PreparerAsync()
    {
        var choisis = Produits.Where(p => p.Choisi).Select(p => p.Produit.Id).ToList();

        if (choisis.Count == 0)
        {
            _dialogue.Avertissement("Cochez au moins un produit avant de préparer la planche.");
            return;
        }

        if (Exemplaires < 1)
        {
            Exemplaires = 1;
        }

        await ExecuterAsync(async () =>
        {
            var etiquettes = await _codes.EtiquettesAsync(new EtiquettesRequete
            {
                ProduitIds = choisis,
                Exemplaires = Exemplaires
            });

            Etiquettes.Clear();
            foreach (var etiquette in etiquettes)
            {
                Etiquettes.Add(etiquette);
            }
        });

        OnPropertyChanged(nameof(AucuneEtiquette));

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>
    /// Produit la planche au format A4 et l'ouvre : l'impression se fait
    /// ensuite depuis le lecteur de PDF, avec ses réglages de papier.
    /// </summary>
    [RelayCommand]
    private async Task ImprimerAsync()
    {
        if (Etiquettes.Count == 0)
        {
            _dialogue.Avertissement("Préparez d'abord la planche d'étiquettes.");
            return;
        }

        await ExecuterAsync(async () =>
        {
            var contenu = await _documents.EtiquettesPdfAsync(Etiquettes.ToList());

            if (_fichiers.DemanderOuEnregistrer("etiquettes.pdf", "Document PDF (*.pdf)|*.pdf")
                is not { } chemin)
            {
                return;
            }

            await File.WriteAllBytesAsync(chemin, contenu);
            _fichiers.Ouvrir(chemin);
        });

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Tient à jour le compteur affiché sur le bouton d'impression.</summary>
    public void SignalerChoix() => OnPropertyChanged(nameof(NombreChoisis));
}

/// <summary>Un produit proposé à l'étiquetage, avec l'état de sa case à cocher.</summary>
public partial class ChoixProduit : ObservableObject
{
    private readonly Action? _prevenir;

    public ChoixProduit(ProduitDto produit, Action? prevenir = null)
    {
        Produit = produit;
        _prevenir = prevenir;
    }

    public ProduitDto Produit { get; }

    public string Reference => Produit.Reference;

    public string Nom => Produit.Nom;

    public string Categorie => Produit.CategorieNom;

    public string Prix => Application.Common.Formatage.Montant(Produit.PrixVente);

    [ObservableProperty]
    private bool _choisi;

    partial void OnChoisiChanged(bool value) => _prevenir?.Invoke();
}
