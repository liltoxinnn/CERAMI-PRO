using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>Documents numérotés automatiquement par le logiciel.</summary>
public enum TypeDocument
{
    [Libelle("Client")] Client,
    [Libelle("Produit")] Produit,
    [Libelle("Commande")] Commande,
    [Libelle("Vente")] Vente,
    [Libelle("Paiement")] Paiement,
    [Libelle("Dépense")] Depense
}
