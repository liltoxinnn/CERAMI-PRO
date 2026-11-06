using CeramiPro.Application.Common;
using CommunityToolkit.Mvvm.ComponentModel;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Une ligne d'un document en cours de saisie : vente à la caisse, achat
/// chez un fournisseur, ou pièces enfournées.
///
/// La ligne recalcule son total à chaque frappe : le caissier voit le
/// montant évoluer pendant qu'il tape, sans avoir à valider.
/// </summary>
public partial class LigneDocument : ObservableObject
{
    public LigneDocument(int articleId, string nom, string? reference, string? unite = null)
    {
        ArticleId = articleId;
        Nom = nom;
        Reference = reference;
        Unite = unite;
    }

    /// <summary>Produit, matière ou ordre de production concerné.</summary>
    public int ArticleId { get; }

    public string Nom { get; }

    public string? Reference { get; }

    public string? Unite { get; }

    [ObservableProperty]
    private decimal _quantite = 1m;

    [ObservableProperty]
    private decimal _prixUnitaire;

    [ObservableProperty]
    private decimal _remise;

    /// <summary>Total de la ligne, remise déduite. Jamais négatif.</summary>
    public decimal Total => Math.Max(0m, Quantite * PrixUnitaire - Remise);

    public string PrixUnitaireAffiche => Formatage.Montant(PrixUnitaire);

    public string RemiseAffichee => Formatage.Montant(Remise);

    public string TotalAffiche => Formatage.Montant(Total);

    public string QuantiteAffichee => Formatage.Quantite(Quantite, Unite);

    /// <summary>Prévient le document parent qu'il doit refaire ses totaux.</summary>
    public event Action? Recalculer;

    partial void OnQuantiteChanged(decimal value) => Signaler();

    partial void OnPrixUnitaireChanged(decimal value) => Signaler();

    partial void OnRemiseChanged(decimal value) => Signaler();

    private void Signaler()
    {
        OnPropertyChanged(nameof(Total));
        OnPropertyChanged(nameof(TotalAffiche));
        OnPropertyChanged(nameof(PrixUnitaireAffiche));
        OnPropertyChanged(nameof(RemiseAffichee));
        OnPropertyChanged(nameof(QuantiteAffichee));

        Recalculer?.Invoke();
    }
}
