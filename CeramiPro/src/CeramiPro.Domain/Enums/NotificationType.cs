using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

public enum NotificationType
{
    [Libelle("Stock faible")] StockFaible = 0,
    [Libelle("Matière première insuffisante")] MatiereInsuffisante = 1,
    [Libelle("Commande proche de l'échéance")] CommandeEcheance = 2,
    [Libelle("Commande en retard")] CommandeRetard = 3,
    [Libelle("Paiement en attente")] PaiementEnAttente = 4,
    [Libelle("Dette client")] DetteClient = 5,
    [Libelle("Dette fournisseur")] DetteFournisseur = 6,
    [Libelle("Production bloquée")] ProductionBloquee = 7,
    [Libelle("Production en retard")] ProductionRetard = 8,
    [Libelle("Pièces en attente depuis trop longtemps")] AttenteProlongee = 9,
    [Libelle("Information")] Information = 10
}
