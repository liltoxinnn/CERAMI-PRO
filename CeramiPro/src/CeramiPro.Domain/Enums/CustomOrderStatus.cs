using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>
/// Où en est une commande personnalisée : quatre étapes, du message du
/// client à la remise de la pièce.
///
/// Les numéros sont ceux de la version précédente, qui comptait dix étapes
/// (conception, validation, cuisson, décoration, contrôle…) : les commandes
/// déjà enregistrées gardent ainsi leur sens. Les numéros 1, 2, 4, 5 et 6
/// appartenaient aux étapes retirées et ne doivent pas être réattribués.
/// </summary>
public enum CustomOrderStatus
{
    /// <summary>Commande reçue, acompte éventuellement versé, pas encore commencée.</summary>
    [Libelle("Reçue")] Recue = 0,

    /// <summary>La pièce est en cours de fabrication.</summary>
    [Libelle("En fabrication")] EnFabrication = 3,

    /// <summary>La pièce est terminée et attend le client.</summary>
    [Libelle("Prête")] Prete = 7,

    /// <summary>Remise au client.</summary>
    [Libelle("Livrée")] Livree = 8,

    [Libelle("Annulée")] Annulee = 9
}
