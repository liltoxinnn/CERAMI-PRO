using CeramiPro.Application.DTOs.Codes;
using CeramiPro.Application.DTOs.Commercial;

namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Production des documents remis au client : factures et reçus, au format
/// PDF, prêts à imprimer ou à envoyer.
/// </summary>
public interface IDocumentService
{
    /// <summary>Facture complète, au format A4.</summary>
    Task<byte[]> FacturePdfAsync(int factureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Reçu de caisse, au format d'un rouleau de 80 mm : c'est le ticket que
    /// l'on remet au comptoir.
    /// </summary>
    Task<byte[]> RecuPdfAsync(int venteId, CancellationToken cancellationToken = default);

    /// <summary>Enregistre le document et renvoie le chemin du fichier.</summary>
    Task<string> EnregistrerFactureAsync(int factureId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Planche d'étiquettes au format A4, à imprimer sur du papier
    /// autocollant : nom, prix, code-barres et code QR de chaque produit.
    /// </summary>
    Task<byte[]> EtiquettesPdfAsync(
        IReadOnlyList<EtiquetteDto> etiquettes, CancellationToken cancellationToken = default);
}
