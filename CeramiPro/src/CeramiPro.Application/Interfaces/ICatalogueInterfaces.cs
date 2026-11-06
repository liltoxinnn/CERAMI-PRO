using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Catalogue;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Catalogue des produits de l'atelier, et stock de chacun.
///
/// Une pièce faite à la main est unique : il n'y a ni déclinaison ni
/// nomenclature, seulement une fiche, des photos et une quantité.
/// </summary>
public interface IProduitService
{
    Task<PagedResult<ProduitDto>> ListerAsync(
        FiltreProduitsRequete requete, CancellationToken cancellationToken = default);

    Task<ProduitDto> ObtenirAsync(int id, CancellationToken cancellationToken = default);

    Task<ProduitDto> CreerAsync(ProduitRequete requete, CancellationToken cancellationToken = default);

    Task<ProduitDto> ModifierAsync(int id, ProduitRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerAsync(int id, CancellationToken cancellationToken = default);

    Task<SyntheseCatalogueDto> SyntheseAsync(CancellationToken cancellationToken = default);

    /// <summary>Produits passés sous leur seuil d'alerte.</summary>
    Task<IReadOnlyList<ProduitDto>> ListerStockFaibleAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PhotoProduitDto>> ListerPhotosAsync(int produitId, CancellationToken cancellationToken = default);

    Task<PhotoProduitDto> AjouterPhotoAsync(
        int produitId, PhotoProduitRequete requete, CancellationToken cancellationToken = default);

    Task SupprimerPhotoAsync(int produitId, int photoId, CancellationToken cancellationToken = default);
}
