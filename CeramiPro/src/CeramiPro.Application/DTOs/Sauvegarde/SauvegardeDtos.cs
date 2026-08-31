namespace CeramiPro.Application.DTOs.Sauvegarde;

/// <summary>Une sauvegarde présente sur le disque du serveur.</summary>
public record SauvegardeDto(
    string NomFichier,
    long TailleOctets,
    string TailleAffichee,
    DateTime Date,
    bool Automatique);

/// <summary>État du dispositif de sauvegarde, affiché à l'administrateur.</summary>
public record EtatSauvegardeDto(
    bool AutomatiqueActive,
    string HeureAutomatique,
    int ConservationJours,
    string Dossier,
    string NomBaseDeDonnees,
    int Nombre,
    DateTime? DerniereSauvegarde,
    IReadOnlyList<SauvegardeDto> Sauvegardes);

/// <summary>Ce qu'une restauration a réellement remis en place.</summary>
public record RestaurationDto(
    string NomFichier,
    DateTime DateSauvegarde,
    int NombreTables,
    int NombreLignes);
