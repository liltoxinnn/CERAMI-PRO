using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>
/// Par où la commande est arrivée. Une grande partie des commandes
/// personnalisées vient des réseaux sociaux : le savoir aide à répondre au
/// bon endroit, et à voir d'où vient le chiffre d'affaires.
/// </summary>
public enum CanalCommande
{
    [Libelle("À l'atelier")] Atelier = 0,
    [Libelle("Instagram")] Instagram = 1,
    [Libelle("WhatsApp")] WhatsApp = 2,
    [Libelle("Facebook")] Facebook = 3,
    [Libelle("Téléphone")] Telephone = 4,
    [Libelle("Autre")] Autre = 5
}
