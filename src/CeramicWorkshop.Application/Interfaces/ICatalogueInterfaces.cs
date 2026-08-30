using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Catalogue;

namespace CeramicWorkshop.Application.Interfaces;

public interface IProduitService
{
    Task<PagedResult<ProduitDto>> ListerAsync(
        FiltreProduitsRequete requete, CancellationToken cancellationToken = default);

    Task<ProduitDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>Retrouve un produit à partir d'un code-barres ou d'une référence scannée.</summary>
    Task<ProduitDto?> RechercherParCodeAsync(string code, CancellationToken cancellationToken = default);

    Task<ProduitDto> CreerAsync(ProduitRequete requete, CancellationToken cancellationToken = default);

    Task<ProduitDto> ModifierAsync(int id, ProduitRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);

    Task<SyntheseCatalogueDto> SyntheseAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProduitDto>> ListerStockFaibleAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhotoProduitDto>> ListerPhotosAsync(int produitId, CancellationToken cancellationToken = default);

    Task<PhotoProduitDto> AjouterPhotoAsync(
        int produitId, PhotoProduitRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerPhotoAsync(int produitId, int photoId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<VarianteProduitDto>> ListerVariantesAsync(
        int produitId, CancellationToken cancellationToken = default);

    Task<VarianteProduitDto> AjouterVarianteAsync(
        int produitId, VarianteProduitRequete requete, CancellationToken cancellationToken = default);

    Task<VarianteProduitDto> ModifierVarianteAsync(
        int produitId, int varianteId, VarianteProduitRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerVarianteAsync(int produitId, int varianteId, CancellationToken cancellationToken = default);
}

public interface IRecetteService
{
    Task<IReadOnlyList<RecetteDto>> ListerAsync(int? produitId = null, CancellationToken cancellationToken = default);

    Task<RecetteDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<RecetteDto> CreerAsync(RecetteRequete requete, CancellationToken cancellationToken = default);

    Task<RecetteDto> ModifierAsync(int id, RecetteRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calcule les matières nécessaires pour produire une quantité donnée et
    /// compare avec le stock disponible.
    /// </summary>
    Task<BesoinsRecetteDto> CalculerBesoinsAsync(
        int recetteId, decimal quantite, CancellationToken cancellationToken = default);
}
