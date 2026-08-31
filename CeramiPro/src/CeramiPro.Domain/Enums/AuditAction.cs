using CeramiPro.Domain.Common;

namespace CeramiPro.Domain.Enums;

/// <summary>Règle métier n°20 : les opérations importantes sont journalisées.</summary>
public enum AuditAction
{
    [Libelle("Création")] Creation = 0,
    [Libelle("Modification")] Modification = 1,
    [Libelle("Suppression")] Suppression = 2,
    [Libelle("Connexion")] Connexion = 3,
    [Libelle("Déconnexion")] Deconnexion = 4,
    [Libelle("Annulation")] Annulation = 5,
    [Libelle("Dérogation")] Derogation = 6,
    [Libelle("Échec de connexion")] EchecConnexion = 7,
    [Libelle("Sauvegarde")] Sauvegarde = 8
}
