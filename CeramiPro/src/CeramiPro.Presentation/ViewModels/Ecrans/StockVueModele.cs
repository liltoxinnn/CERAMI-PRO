using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Vue générale du stock : ce que l'atelier possède, ce que cela vaut, et ce
/// qui manque.
///
/// Les deux tableaux ne montrent que les articles sous leur seuil : c'est la
/// seule information qui appelle une décision.
/// </summary>
public partial class StockVueModele : VueModeleBase
{
    private readonly IMatiereService _matieres;
    private readonly IProduitService _produits;
    private readonly IServiceLangue _langue;

    public StockVueModele(
        IMatiereService matieres, IProduitService produits, IServiceLangue langue)
    {
        _matieres = matieres;
        _produits = produits;
        _langue = langue;
    }

    public override string Titre => _langue["menu.stock.vueGenerale"];

    public override string Introduction =>
        "Ce que l'atelier possède, ce que cela vaut, et ce qui est sur le point de manquer.";

    [ObservableProperty]
    private string _valeurMatieres = string.Empty;

    [ObservableProperty]
    private string _valeurProduits = string.Empty;

    [ObservableProperty]
    private string _valeurTotale = string.Empty;

    [ObservableProperty]
    private int _nombreMatieres;

    [ObservableProperty]
    private int _nombreProduits;

    [ObservableProperty]
    private int _matieresFaibles;

    [ObservableProperty]
    private int _produitsFaibles;

    /// <summary>Matières passées sous leur seuil d'alerte.</summary>
    public ObservableCollection<MatiereDto> AlertesMatieres { get; } = new();

    /// <summary>Produits finis passés sous leur seuil d'alerte.</summary>
    public ObservableCollection<ProduitDto> AlertesProduits { get; } = new();

    public bool StockSain => !ChargementEnCours && MatieresFaibles == 0 && ProduitsFaibles == 0;

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var stock = await _matieres.SyntheseAsync();
            var catalogue = await _produits.SyntheseAsync();

            NombreMatieres = stock.NombreArticles;
            MatieresFaibles = stock.NombreStockFaible;
            NombreProduits = catalogue.NombreProduits;
            ProduitsFaibles = catalogue.NombreStockFaible;

            ValeurMatieres = Formatage.Montant(stock.ValeurTotale);
            ValeurProduits = Formatage.Montant(catalogue.ValeurStock);
            ValeurTotale = Formatage.Montant(stock.ValeurTotale + catalogue.ValeurStock);

            AlertesMatieres.Clear();
            foreach (var matiere in await _matieres.ListerStockFaibleAsync())
            {
                AlertesMatieres.Add(matiere);
            }

            AlertesProduits.Clear();
            foreach (var produit in await _produits.ListerStockFaibleAsync())
            {
                AlertesProduits.Add(produit);
            }
        });

        OnPropertyChanged(nameof(StockSain));
    }

    [RelayCommand]
    private Task ActualiserAsync() => ChargerAsync();
}
