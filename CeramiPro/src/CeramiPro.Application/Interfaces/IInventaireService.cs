using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Domain.Entities.Inventory;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Point de passage unique pour toute variation de stock (règle métier n°2).
/// Aucun autre service ne modifie directement une quantité en stock.
/// </summary>
public interface IInventaireService
{
    /// <summary>
    /// Enregistre un mouvement et met à jour la quantité de l'article.
    /// L'enregistrement en base est effectué par l'appelant.
    /// </summary>
    Task<InventoryTransaction> EnregistrerAsync(
        MouvementStockRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Annule les mouvements d'un document en créant les mouvements inverses.</summary>
    Task<IReadOnlyList<InventoryTransaction>> AnnulerDocumentAsync(
        int? achatId, int? venteId, int? productionId, string motif,
        CancellationToken cancellationToken = default);

    Task<PagedResult<MouvementStockDto>> ListerAsync(
        FiltreMouvementsRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Enregistre une régularisation justifiée après comptage.</summary>
    Task<MouvementStockDto> RegulariserAsync(
        RegularisationRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Indique si l'atelier accepte un stock négatif (déconseillé).</summary>
    Task<bool> StockNegatifAutoriseAsync(CancellationToken cancellationToken = default);
}
