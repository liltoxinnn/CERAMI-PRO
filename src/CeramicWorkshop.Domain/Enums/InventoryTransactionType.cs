using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

/// <summary>
/// Règle métier n°2 : aucun changement de stock ne doit être effectué silencieusement.
/// Chaque mouvement porte un type explicite.
/// </summary>
public enum InventoryTransactionType
{
    [Libelle("Achat")] Achat = 0,
    [Libelle("Consommation en production")] ConsommationProduction = 1,
    [Libelle("Entrée de production")] EntreeProduction = 2,
    [Libelle("Vente")] Vente = 3,
    [Libelle("Retour")] Retour = 4,
    [Libelle("Ajustement")] Ajustement = 5,
    [Libelle("Produit endommagé")] Endommage = 6,
    [Libelle("Transfert")] Transfert = 7,
    [Libelle("Annulation")] Annulation = 8
}
