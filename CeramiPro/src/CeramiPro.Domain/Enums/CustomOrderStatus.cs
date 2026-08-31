using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>Suivi d'une commande personnalisée, de la demande à la livraison.</summary>
public enum CustomOrderStatus
{
    [Libelle("Commande")] Commande = 0,
    [Libelle("Conception")] Conception = 1,
    [Libelle("Validation client")] ValidationClient = 2,
    [Libelle("Production")] Production = 3,
    [Libelle("Cuisson")] Cuisson = 4,
    [Libelle("Décoration")] Decoration = 5,
    [Libelle("Contrôle qualité")] ControleQualite = 6,
    [Libelle("Prêt")] Pret = 7,
    [Libelle("Livré")] Livre = 8,
    [Libelle("Annulé")] Annule = 9
}
