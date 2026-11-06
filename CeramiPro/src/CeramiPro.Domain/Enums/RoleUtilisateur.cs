using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>
/// Rôle d'une personne qui emploie le logiciel.
///
/// Deux rôles suffisent à un atelier : celui qui tient la boutique, et celui
/// qui tient les comptes. Un découpage plus fin se paie en réglages à faire
/// et en questions à se poser, pour un atelier qui compte deux ou trois
/// personnes se connaissant toutes.
/// </summary>
public enum RoleUtilisateur
{
    /// <summary>Ventes, commandes, clients et produits. Ni rapports ni réglages.</summary>
    [Libelle("Employé")] Employe = 0,

    /// <summary>Tout, y compris les rapports, les dépenses et les paramètres.</summary>
    [Libelle("Administrateur")] Administrateur = 1
}
