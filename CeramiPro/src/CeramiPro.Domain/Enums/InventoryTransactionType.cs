using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>
/// Nature d'un mouvement de stock. Chaque mouvement porte la sienne : un
/// stock qui baisse sans que l'on sache pourquoi ne s'explique plus.
///
/// Les valeurs numériques sont celles de la version précédente : les
/// mouvements déjà enregistrés et les anciennes sauvegardes gardent ainsi
/// leur sens. Les numéros 1, 2 et 7 appartenaient aux modules retirés
/// (production, transferts) et ne doivent pas être réattribués.
/// </summary>
public enum InventoryTransactionType
{
    /// <summary>Pièces fabriquées ou reçues, entrant en stock.</summary>
    [Libelle("Entrée en stock")] Entree = 0,

    /// <summary>Vente au comptoir ou sur commande.</summary>
    [Libelle("Vente")] Vente = 3,

    /// <summary>Pièce rendue par un client, remise en stock.</summary>
    [Libelle("Retour client")] Retour = 4,

    /// <summary>Correction après comptage, avec motif obligatoire.</summary>
    [Libelle("Régularisation")] Regularisation = 5,

    /// <summary>Pièce cassée ou perdue.</summary>
    [Libelle("Casse ou perte")] Perte = 6,

    /// <summary>Inversion d'un mouvement précédent, après annulation d'une vente.</summary>
    [Libelle("Annulation")] Annulation = 8
}
