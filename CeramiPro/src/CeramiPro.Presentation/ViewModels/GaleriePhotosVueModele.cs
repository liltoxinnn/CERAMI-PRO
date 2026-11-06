using System.Collections.ObjectModel;
using CeramiPro.Application.DTOs.Catalogue;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>Une photo affichée dans la galerie.</summary>
/// <param name="Chemin">Chemin enregistré en base, relatif au dossier des photos.</param>
/// <param name="CheminComplet">Fichier sur le disque, pour l'affichage.</param>
public record PhotoAffichee(int Id, string Chemin, string CheminComplet, string? Legende, bool Principale)
{
    public bool Introuvable => !File.Exists(CheminComplet);
}

/// <summary>D'où viennent les photos de la galerie : un produit, une commande.</summary>
public interface ISourcePhotos
{
    string Titre { get; }

    string Introduction { get; }

    /// <summary>Sous-dossier où ranger les fichiers : « produits/12 ».</summary>
    string SousDossier { get; }

    /// <summary>Vrai quand l'une des photos représente la fiche (produits).</summary>
    bool GerePrincipale { get; }

    Task<IReadOnlyList<(int Id, string Chemin, string? Legende, bool Principale)>> ListerAsync();

    Task AjouterAsync(string chemin);

    /// <summary>Retire la photo de la fiche et renvoie le chemin du fichier.</summary>
    Task<string> SupprimerAsync(int id);

    Task DefinirPrincipaleAsync(int id);
}

/// <summary>
/// Photos d'un produit ou d'une commande : ajouter depuis le disque,
/// choisir celle qui représente le produit, retirer, ouvrir en grand.
///
/// Une pièce faite à la main se vend d'abord par son image ; une commande
/// reçue sur Instagram arrive souvent avec une capture d'écran du modèle.
/// </summary>
public partial class GaleriePhotosVueModele : VueModeleBase
{
    private const string FiltreImages =
        "Photos (*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif)|*.jpg;*.jpeg;*.png;*.webp;*.bmp;*.gif";

    private readonly ISourcePhotos _source;
    private readonly IStockagePhotos _stockage;
    private readonly IServiceFichier _fichiers;
    private readonly IServiceDialogue _dialogue;

    public GaleriePhotosVueModele(
        ISourcePhotos source, IStockagePhotos stockage, IServiceFichier fichiers, IServiceDialogue dialogue)
    {
        _source = source;
        _stockage = stockage;
        _fichiers = fichiers;
        _dialogue = dialogue;
    }

    public override string Titre => _source.Titre;

    public override string Introduction => _source.Introduction;

    public bool GerePrincipale => _source.GerePrincipale;

    public ObservableCollection<PhotoAffichee> Photos { get; } = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(PhotoEstChoisie))]
    private PhotoAffichee? _photoChoisie;

    public bool PhotoEstChoisie => PhotoChoisie is not null;

    public bool AucunePhoto => !ChargementEnCours && Photos.Count == 0;

    public string Compte => Photos.Count switch
    {
        0 => "Aucune photo",
        1 => "1 photo",
        var n => $"{n} photos"
    };

    public override async Task ChargerAsync()
    {
        await ExecuterAsync(async () =>
        {
            var choisie = PhotoChoisie?.Id;

            Photos.Clear();

            foreach (var (id, chemin, legende, principale) in await _source.ListerAsync())
            {
                Photos.Add(new PhotoAffichee(id, chemin, _stockage.CheminComplet(chemin), legende, principale));
            }

            PhotoChoisie = Photos.FirstOrDefault(p => p.Id == choisie) ?? Photos.FirstOrDefault();
        });

        OnPropertyChanged(nameof(AucunePhoto));
        OnPropertyChanged(nameof(Compte));
    }

    [RelayCommand]
    private async Task AjouterAsync()
    {
        var fichiers = _fichiers.Choisir("Ajouter des photos", FiltreImages);

        if (fichiers.Count == 0)
        {
            return;
        }

        var refusees = new List<string>();

        await ExecuterAsync(async () =>
        {
            foreach (var fichier in fichiers)
            {
                string? copie = null;

                try
                {
                    copie = await _stockage.ImporterAsync(fichier, _source.SousDossier);
                    await _source.AjouterAsync(copie);
                }
                catch (Application.Common.RegleMetierException erreur)
                {
                    refusees.Add(erreur.Message);

                    if (copie is not null)
                    {
                        _stockage.Supprimer(copie);
                    }
                }
            }
        });

        await ChargerAsync();

        if (refusees.Count > 0)
        {
            _dialogue.Avertissement(string.Join("\n\n", refusees), "Photos non ajoutées");
        }
        else if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    [RelayCommand]
    private async Task DefinirPrincipaleAsync()
    {
        if (PhotoChoisie is not { } photo || photo.Principale)
        {
            return;
        }

        await ExecuterAsync(() => _source.DefinirPrincipaleAsync(photo.Id));
        await ChargerAsync();
    }

    [RelayCommand]
    private async Task SupprimerAsync()
    {
        if (PhotoChoisie is not { } photo)
        {
            return;
        }

        if (!_dialogue.Confirmer("Retirer cette photo ? Le fichier sera effacé du dossier des photos."))
        {
            return;
        }

        await ExecuterAsync(async () =>
        {
            var chemin = await _source.SupprimerAsync(photo.Id);
            _stockage.Supprimer(chemin);
        });

        PhotoChoisie = null;
        await ChargerAsync();

        if (MessageErreur is not null)
        {
            _dialogue.Erreur(MessageErreur);
        }
    }

    /// <summary>Ouvre la photo choisie dans la visionneuse de Windows.</summary>
    [RelayCommand]
    private void Ouvrir()
    {
        if (PhotoChoisie is not { } photo)
        {
            return;
        }

        if (photo.Introuvable)
        {
            _dialogue.Avertissement(
                $"Le fichier de cette photo est introuvable :\n{photo.CheminComplet}\n\n" +
                "Il a peut-être été déplacé. Retirez la photo puis ajoutez-la de nouveau.");
            return;
        }

        _fichiers.Ouvrir(photo.CheminComplet);
    }
}

/// <summary>Construit la galerie d'un produit ou d'une commande.</summary>
public class FabriqueGaleries
{
    private readonly IProduitService _produits;
    private readonly ICommandeService _commandes;
    private readonly IStockagePhotos _stockage;
    private readonly IServiceFichier _fichiers;
    private readonly IServiceDialogue _dialogue;

    public FabriqueGaleries(
        IProduitService produits, ICommandeService commandes, IStockagePhotos stockage,
        IServiceFichier fichiers, IServiceDialogue dialogue)
    {
        _produits = produits;
        _commandes = commandes;
        _stockage = stockage;
        _fichiers = fichiers;
        _dialogue = dialogue;
    }

    public GaleriePhotosVueModele PourProduit(ProduitDto produit)
        => new(new PhotosProduit(_produits, produit), _stockage, _fichiers, _dialogue);

    public GaleriePhotosVueModele PourCommande(CommandeDto commande)
        => new(new PhotosCommande(_commandes, commande), _stockage, _fichiers, _dialogue);

    private sealed class PhotosProduit : ISourcePhotos
    {
        private readonly IProduitService _service;
        private readonly ProduitDto _produit;

        public PhotosProduit(IProduitService service, ProduitDto produit)
        {
            _service = service;
            _produit = produit;
        }

        public string Titre => $"Photos — {_produit.Nom}";

        public string Introduction =>
            "La photo principale représente le produit dans le catalogue. Les fichiers sont recopiés " +
            "dans le dossier des photos de CeramiPro et inclus dans les sauvegardes.";

        public string SousDossier => $"produits/{_produit.Id}";

        public bool GerePrincipale => true;

        public async Task<IReadOnlyList<(int Id, string Chemin, string? Legende, bool Principale)>> ListerAsync()
            => (await _service.ListerPhotosAsync(_produit.Id))
                .Select(p => (p.Id, p.Chemin, p.Legende, p.Principale))
                .ToList();

        public Task AjouterAsync(string chemin)
            => _service.AjouterPhotoAsync(_produit.Id, new PhotoProduitRequete { Chemin = chemin });

        public Task<string> SupprimerAsync(int id) => _service.SupprimerPhotoAsync(_produit.Id, id);

        public Task DefinirPrincipaleAsync(int id) => _service.DefinirPhotoPrincipaleAsync(_produit.Id, id);
    }

    private sealed class PhotosCommande : ISourcePhotos
    {
        private readonly ICommandeService _service;
        private readonly CommandeDto _commande;

        public PhotosCommande(ICommandeService service, CommandeDto commande)
        {
            _service = service;
            _commande = commande;
        }

        public string Titre => $"Photos — commande {_commande.Numero}";

        public string Introduction =>
            "Modèle envoyé par le client, croquis, photos de la pièce en cours : tout ce qui aide " +
            "à faire exactement ce qui a été demandé.";

        public string SousDossier => $"commandes/{_commande.Id}";

        public bool GerePrincipale => false;

        public async Task<IReadOnlyList<(int Id, string Chemin, string? Legende, bool Principale)>> ListerAsync()
            => (await _service.ObtenirAsync(_commande.Id)).Photos
                .Select(p => (p.Id, p.Chemin, p.Legende, false))
                .ToList();

        public Task AjouterAsync(string chemin)
            => _service.AjouterPhotoAsync(_commande.Id, new PhotoCommandeRequete
            {
                Chemin = chemin,
                Type = CustomOrderImageKind.Reference
            });

        public Task<string> SupprimerAsync(int id) => _service.SupprimerPhotoAsync(_commande.Id, id);

        public Task DefinirPrincipaleAsync(int id) => Task.CompletedTask;
    }
}
