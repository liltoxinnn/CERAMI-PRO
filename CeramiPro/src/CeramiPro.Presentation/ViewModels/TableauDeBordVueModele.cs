using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Ecrans;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Écran d'accueil : ce qu'il faut savoir en ouvrant l'atelier le matin.
///
/// Les chiffres viennent du même calcul que les rapports — ventes et
/// commandes livrées, à l'heure d'Alger — et rien n'est affiché qui n'ait
/// été lu dans la base.
/// </summary>
public partial class TableauDeBordVueModele : VueModeleBase
{
    private readonly ITableauDeBordService _tableau;
    private readonly IServiceNavigation _navigation;
    private readonly OutilsListe _outils;

    public TableauDeBordVueModele(
        ITableauDeBordService tableau, IServiceNavigation navigation, OutilsListe outils)
    {
        _tableau = tableau;
        _navigation = navigation;
        _outils = outils;
    }

    public override string Titre => "Tableau de bord";

    public override string Introduction =>
        "L'activité du jour et du mois, les commandes à suivre et le stock à surveiller.";

    /// <summary>Faux tant que rien n'a été lu, pour ne pas afficher de zéros trompeurs.</summary>
    [ObservableProperty]
    private bool _estCharge;

    // ------------------------------------------------------------ Indicateurs

    [ObservableProperty] private string _chiffreDuJour = string.Empty;
    [ObservableProperty] private string _detailDuJour = string.Empty;
    [ObservableProperty] private string _encaisseDuJour = string.Empty;
    [ObservableProperty] private string _chiffreDuMois = string.Empty;
    [ObservableProperty] private string _detailDuMois = string.Empty;
    [ObservableProperty] private string _resultatDuMois = string.Empty;
    [ObservableProperty] private string _detailResultat = string.Empty;
    [ObservableProperty] private bool _resultatNegatif;

    // --------------------------------------------------------------- Commandes

    [ObservableProperty] private int _commandesRecues;
    [ObservableProperty] private int _commandesEnCours;
    [ObservableProperty] private int _commandesProchesEcheance;
    [ObservableProperty] private int _commandesEnRetard;
    [ObservableProperty] private int _commandesPretes;

    // ------------------------------------------------------------ Stock, argent

    [ObservableProperty] private int _nombreProduits;
    [ObservableProperty] private int _produitsASurveiller;
    [ObservableProperty] private string _valeurStock = string.Empty;
    [ObservableProperty] private string _resteAEncaisser = string.Empty;
    [ObservableProperty] private string _acomptesRecus = string.Empty;

    /// <summary>Produits passés sous leur seuil d'alerte.</summary>
    public ObservableCollection<LigneTableau> StockFaible { get; } = new();

    /// <summary>Les pièces qui sont parties le plus ces trente derniers jours.</summary>
    public ObservableCollection<LigneTableau> PlusVendus { get; } = new();

    public bool AucunStockFaible => EstCharge && StockFaible.Count == 0;

    public bool AucuneVenteRecente => EstCharge && PlusVendus.Count == 0;

    // -------------------------------------------------------------- Graphiques

    public ObservableCollection<BarreGraphique> VentesTrenteJours { get; } = new();

    public ObservableCollection<BarreGraphique> VentesDouzeMois { get; } = new();

    /// <summary>Valeur de la plus haute barre, rappelée au-dessus du graphique.</summary>
    [ObservableProperty] private string _maximumTrenteJours = string.Empty;
    [ObservableProperty] private string _maximumDouzeMois = string.Empty;

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var donnees = await _tableau.ObtenirAsync();

            var jour = donnees.Aujourdhui;
            ChiffreDuJour = Formatage.Montant(jour.ChiffreAffaires);
            DetailDuJour = Pluriel(jour.NombreVentes, "vente", "ventes")
                           + " · " + Pluriel(jour.CommandesLivrees, "commande livrée", "commandes livrées");
            EncaisseDuJour = Formatage.Montant(jour.Encaisse);

            var mois = donnees.Mois;
            ChiffreDuMois = Formatage.Montant(mois.ChiffreAffaires);
            DetailDuMois = $"Bénéfice : {Formatage.Montant(mois.Benefice)} · "
                           + Pluriel(mois.NombreVentes, "vente", "ventes");
            ResultatDuMois = Formatage.Montant(mois.Resultat);
            DetailResultat = $"Après {Formatage.Montant(mois.Depenses)} de dépenses";
            ResultatNegatif = mois.Resultat < 0;

            CommandesRecues = donnees.Commandes.EnAttente;
            CommandesEnCours = donnees.Commandes.EnCours;
            CommandesProchesEcheance = donnees.Commandes.ProchesEcheance;
            CommandesEnRetard = donnees.Commandes.EnRetard;
            CommandesPretes = donnees.Commandes.PretesALivrer;

            NombreProduits = donnees.Stock.NombreProduits;
            ProduitsASurveiller = donnees.Stock.ProduitsFaibles;
            ValeurStock = Formatage.Montant(donnees.Stock.ValeurStock);
            ResteAEncaisser = Formatage.Montant(donnees.Finances.ResteAEncaisser);
            AcomptesRecus = Formatage.Montant(donnees.Finances.AcomptesRecus);

            Remplir(StockFaible, donnees.StockFaible.Select(p => new LigneTableau(
                p.Nom,
                $"{Formatage.Quantite(p.Stock)} en stock",
                $"seuil {Formatage.Quantite(p.Seuil)}")));

            Remplir(PlusVendus, donnees.ProduitsLesPlusVendus.Select(p => new LigneTableau(
                p.Nom,
                Pluriel((int)Math.Round(p.Quantite), "pièce", "pièces"),
                Formatage.Montant(p.Montant))));

            // Une étiquette sur cinq sous le graphique des jours : trente dates
            // côte à côte ne se liraient plus.
            MaximumTrenteJours = Graphique.Remplir(VentesTrenteJours, donnees.VentesParJour, etiquetteTousLes: 5);
            MaximumDouzeMois = Graphique.Remplir(VentesDouzeMois, donnees.VentesParMois);

            EstCharge = true;
        });

        OnPropertyChanged(nameof(AucunStockFaible));
        OnPropertyChanged(nameof(AucuneVenteRecente));
    }

    [RelayCommand]
    private Task ActualiserAsync() => ChargerAsync();

    // ---------------------------------------------------------- Actions rapides

    [RelayCommand]
    private void NouvelleVente() => _navigation.Naviguer<CaisseVueModele>();

    [RelayCommand]
    private Task NouvelleCommandeAsync() => OuvrirPuisRechargerAsync<CommandeFormulaireVueModele>();

    [RelayCommand]
    private Task NouveauProduitAsync() => OuvrirPuisRechargerAsync<ProduitFormulaireVueModele>();

    [RelayCommand]
    private void VoirCommandes() => _navigation.Naviguer<CommandesVueModele>();

    [RelayCommand]
    private void VoirStock() => _navigation.Naviguer<ProduitsStockVueModele>();

    [RelayCommand]
    private void VoirRapports() => _navigation.Naviguer<RapportsVueModele>();

    private async Task OuvrirPuisRechargerAsync<TFormulaire>() where TFormulaire : class, IFormulaire
    {
        var enregistre = false;

        await ExecuterAsync(async () => enregistre = await _outils.OuvrirFormulaireAsync<TFormulaire>());

        if (MessageErreur is not null)
        {
            _outils.Dialogue.Erreur(MessageErreur);
        }
        else if (enregistre)
        {
            await ChargerAsync();
        }
    }

    // ----------------------------------------------------------------- Outils

    private static void Remplir<T>(ObservableCollection<T> cible, IEnumerable<T> source)
    {
        cible.Clear();

        foreach (var element in source)
        {
            cible.Add(element);
        }
    }

    /// <summary>« aucune vente », « 1 pièce », « 3 commandes livrées » — les noms employés sont féminins.</summary>
    private static string Pluriel(int nombre, string singulier, string pluriel)
        => nombre switch
        {
            0 => $"aucune {singulier}",
            1 => $"1 {singulier}",
            _ => $"{nombre} {pluriel}"
        };
}

/// <summary>Une ligne d'une liste du tableau de bord : un nom et deux valeurs déjà mises en forme.</summary>
public record LigneTableau(string Nom, string Valeur, string Complement);
