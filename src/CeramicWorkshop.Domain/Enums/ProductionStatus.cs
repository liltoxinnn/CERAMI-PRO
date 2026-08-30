using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

/// <summary>Étapes de fabrication d'un ordre de production (workflow atelier).</summary>
public enum ProductionStatus
{
    [Libelle("Planifié")] Planifie = 0,
    [Libelle("Préparation")] Preparation = 1,
    [Libelle("Façonnage")] Faconnage = 2,
    [Libelle("Séchage")] Sechage = 3,
    [Libelle("Première cuisson")] PremiereCuisson = 4,
    [Libelle("Décoration")] Decoration = 5,
    [Libelle("Cuisson finale")] CuissonFinale = 6,
    [Libelle("Contrôle qualité")] ControleQualite = 7,
    [Libelle("Terminé")] Termine = 8,
    [Libelle("Annulé")] Annule = 9
}
