namespace CeramiPro.Application.Interfaces;

/// <summary>
/// Documents remis au client, au format PDF.
///
/// L'atelier délivre un reçu de caisse, qui porte les mentions légales de
/// l'entreprise — identifiant fiscal, registre du commerce, article
/// d'imposition — saisies dans les paramètres.
/// </summary>
public interface IDocumentService
{
    /// <summary>Reçu d'une vente, au format d'un rouleau de 80 mm.</summary>
    Task<byte[]> RecuVentePdfAsync(int venteId, CancellationToken cancellationToken = default);

    /// <summary>Reçu d'un acompte ou d'un solde versé sur une commande.</summary>
    Task<byte[]> RecuCommandePdfAsync(int commandeId, CancellationToken cancellationToken = default);
}
