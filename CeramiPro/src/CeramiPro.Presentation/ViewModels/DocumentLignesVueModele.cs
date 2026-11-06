using System.Collections.ObjectModel;
using CeramiPro.Application.Common;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Base des écrans qui composent un document ligne par ligne, comme la
/// caisse : on choisit un article, on donne une quantité, on l'ajoute, et le
/// total se met à jour. Ce qui est propre au document — les champs
/// d'en-tête, ce qu'on enregistre — reste dans la classe dérivée.
/// </summary>
public abstract partial class DocumentLignesVueModele<TRequete> : VueModeleBase
    where TRequete : class, new()
{
    protected readonly IServiceDialogue Dialogue;

    protected DocumentLignesVueModele(IServiceDialogue dialogue)
    {
        Dialogue = dialogue;

        Lignes.CollectionChanged += (_, _) => Recalculer();
    }

    /// <summary>En-tête du document, lié aux champs déclarés par l'écran.</summary>
    public TRequete Requete { get; protected set; } = new();

    /// <summary>Champs d'en-tête : client, mode de règlement, notes…</summary>
    public abstract IReadOnlyList<ChampFormulaire> Champs { get; }

    /// <summary>Nom donné à ce que l'on ajoute : « produit ».</summary>
    public abstract string NomArticle { get; }

    /// <summary>Intitulé du bouton d'enregistrement.</summary>
    public abstract string LibelleEnregistrer { get; }

    /// <summary>Faux pour un document où les pièces n'ont pas de prix.</summary>
    public virtual bool AfficherPrix => true;

    /// <summary>Vrai à la caisse, où l'on encaisse en même temps que l'on vend.</summary>
    public virtual bool GereReglement => false;

    // ------------------------------------------------------------- Articles

    /// <summary>Articles proposés dans la liste déroulante.</summary>
    public ObservableCollection<OptionChamp> Articles { get; } = new();

    [ObservableProperty]
    private int? _articleChoisi;

    [ObservableProperty]
    private decimal _quantite = 1m;

    [ObservableProperty]
    private decimal _prixUnitaire;

    [ObservableProperty]
    private decimal _remiseLigne;

    partial void OnArticleChoisiChanged(int? value) => _ = ArticleChoisiAsync(value);

    /// <summary>
    /// Appelé quand l'utilisateur choisit un article : sert à proposer son
    /// prix habituel, pour ne pas avoir à le retaper.
    /// </summary>
    protected virtual Task ArticleChoisiAsync(int? articleId) => Task.CompletedTask;

    // -------------------------------------------------------------- Lignes

    public ObservableCollection<LigneDocument> Lignes { get; } = new();

    [ObservableProperty]
    private LigneDocument? _ligneSelectionnee;

    public bool AucuneLigne => Lignes.Count == 0;

    public int NombreLignes => Lignes.Count;

    // -------------------------------------------------------------- Totaux

    [ObservableProperty]
    private decimal _remiseDocument;

    partial void OnRemiseDocumentChanged(decimal value) => Recalculer();

    public decimal SousTotal => Lignes.Sum(l => l.Total);

    public decimal Total => Math.Max(0m, SousTotal - RemiseDocument);

    public string SousTotalAffiche => Formatage.Montant(SousTotal);

    public string RemiseAffichee => Formatage.Montant(RemiseDocument);

    public string TotalAffiche => Formatage.Montant(Total);

    /// <summary>Quantité totale de pièces, utile quand il n'y a pas de prix.</summary>
    public string QuantiteTotaleAffichee => Formatage.Quantite(Lignes.Sum(l => l.Quantite));

    // ------------------------------------------------------------ Règlement

    [ObservableProperty]
    private decimal _montantPaye;

    partial void OnMontantPayeChanged(decimal value) => RecalculerReglement();

    /// <summary>Monnaie à rendre ; zéro tant que le client n'a pas assez donné.</summary>
    public decimal Rendu => Math.Max(0m, MontantPaye - Total);

    /// <summary>Ce qu'il reste à devoir après ce règlement.</summary>
    public decimal Reste => Math.Max(0m, Total - MontantPaye);

    public string RenduAffiche => Formatage.Montant(Rendu);

    public string ResteAffiche => Formatage.Montant(Reste);

    // ------------------------------------------------------------ Commandes

    /// <summary>Ajoute l'article choisi au document.</summary>
    [RelayCommand]
    private void AjouterLigne()
    {
        MessageErreur = null;

        if (ArticleChoisi is not { } articleId)
        {
            MessageErreur = $"Choisissez d'abord un {NomArticle}.";
            return;
        }

        if (Quantite <= 0m)
        {
            MessageErreur = "La quantité doit être supérieure à zéro.";
            return;
        }

        var option = Articles.FirstOrDefault(a => a.Valeur == articleId);

        Ajouter(articleId, option?.Libelle ?? NomArticle, Quantite, PrixUnitaire, RemiseLigne);

        // Prêt pour la ligne suivante : on efface la saisie, pas le panier.
        ArticleChoisi = null;
        Quantite = 1m;
        PrixUnitaire = 0m;
        RemiseLigne = 0m;
    }

    /// <summary>
    /// Ajoute une ligne, ou augmente celle qui existe déjà pour cet article.
    /// Ajouter deux fois le même produit doit donner « 2 », pas deux lignes.
    /// </summary>
    protected void Ajouter(int articleId, string nom, decimal quantite,
        decimal prixUnitaire, decimal remise = 0m, string? reference = null, string? unite = null)
    {
        if (Lignes.FirstOrDefault(l => l.ArticleId == articleId) is { } existante)
        {
            existante.Quantite += quantite;
            return;
        }

        var ligne = new LigneDocument(articleId, nom, reference, unite)
        {
            Quantite = quantite,
            PrixUnitaire = prixUnitaire,
            Remise = remise
        };

        ligne.Recalculer += Recalculer;
        Lignes.Add(ligne);
    }

    [RelayCommand]
    private void RetirerLigne()
    {
        if (LigneSelectionnee is not { } ligne)
        {
            MessageErreur = "Choisissez d'abord une ligne à retirer.";
            return;
        }

        ligne.Recalculer -= Recalculer;
        Lignes.Remove(ligne);
        LigneSelectionnee = null;
    }

    [RelayCommand]
    private void Vider()
    {
        if (Lignes.Count == 0)
        {
            return;
        }

        if (!Dialogue.Confirmer("Vider le document en cours ? Les lignes saisies seront perdues."))
        {
            return;
        }

        Reinitialiser();
    }

    /// <summary>Repart d'un document vide, après enregistrement ou abandon.</summary>
    protected void Reinitialiser()
    {
        foreach (var ligne in Lignes)
        {
            ligne.Recalculer -= Recalculer;
        }

        Lignes.Clear();

        Requete = new TRequete();
        ArticleChoisi = null;
        Quantite = 1m;
        PrixUnitaire = 0m;
        RemiseLigne = 0m;
        RemiseDocument = 0m;
        MontantPaye = 0m;
        MessageErreur = null;

        OnPropertyChanged(nameof(Requete));
        Recalculer();
    }

    /// <summary>Enregistre le document auprès du service métier.</summary>
    [RelayCommand]
    private async Task EnregistrerAsync()
    {
        MessageErreur = null;

        if (Lignes.Count == 0)
        {
            MessageErreur = $"Ajoutez au moins un {NomArticle} avant d'enregistrer.";
            return;
        }

        if (Verifier() is { } probleme)
        {
            MessageErreur = probleme;
            return;
        }

        await ExecuterAsync(ValiderAsync);

        if (MessageErreur is not null)
        {
            Dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Contrôles propres à l'écran, avant d'appeler le service.</summary>
    protected virtual string? Verifier() => null;

    /// <summary>Enregistrement effectif ; c'est ici que l'écran appelle son service.</summary>
    protected abstract Task ValiderAsync();

    /// <summary>Refait tous les totaux affichés.</summary>
    protected void Recalculer()
    {
        OnPropertyChanged(nameof(SousTotal));
        OnPropertyChanged(nameof(SousTotalAffiche));
        OnPropertyChanged(nameof(RemiseAffichee));
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalAffiche));
        OnPropertyChanged(nameof(QuantiteTotaleAffichee));
        OnPropertyChanged(nameof(AucuneLigne));
        OnPropertyChanged(nameof(NombreLignes));

        RecalculerReglement();
    }

    private void RecalculerReglement()
    {
        OnPropertyChanged(nameof(Rendu));
        OnPropertyChanged(nameof(RenduAffiche));
        OnPropertyChanged(nameof(Reste));
        OnPropertyChanged(nameof(ResteAffiche));
    }
}
