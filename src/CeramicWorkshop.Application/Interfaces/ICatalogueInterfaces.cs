using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Catalogue;
using CeramicWorkshop.Application.DTOs.Codes;
using CeramicWorkshop.Application.DTOs.Recherche;

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

/// <summary>
/// Fabrication des images de codes (QR et code-barres). L'implémentation vit
/// dans la couche Infrastructure : la couche métier n'en connaît que le contrat.
/// </summary>
public interface ICodeGraphiqueService
{
    /// <summary>Code QR au format SVG, prêt à être inséré dans une page.</summary>
    string QrEnSvg(string valeur, int tailleEnPixels = 160);

    /// <summary>Code-barres Code 39 au format SVG, lisible par les douchettes USB.</summary>
    string CodeBarresEnSvg(string valeur, int hauteurEnPixels = 60);

    /// <summary>Indique si la valeur peut être imprimée en code-barres Code 39.</summary>
    bool EstImprimableEnCodeBarres(string valeur);
}

/// <summary>Étiquettes des produits et lecture des codes scannés.</summary>
public interface ICodeService
{
    Task<EtiquetteDto> EtiquetteProduitAsync(int produitId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<EtiquetteDto>> EtiquettesAsync(
        EtiquettesRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Reconnaît un code scanné et indique l'écran à ouvrir.</summary>
    Task<ResultatScanDto> ResoudreAsync(string code, CancellationToken cancellationToken = default);
}

/// <summary>
/// Recherche globale : un seul champ pour retrouver n'importe quelle fiche de
/// l'atelier, même quand le nom est mal orthographié.
/// </summary>
public interface IRechercheService
{
    Task<RechercheGlobaleDto> ChercherAsync(
        string terme, int maximumParFamille = 5, CancellationToken cancellationToken = default);
}
