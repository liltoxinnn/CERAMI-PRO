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
