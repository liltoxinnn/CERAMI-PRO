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
/// <param name="AncienneVersion">
/// La sauvegarde venait de la version d'avant la simplification : ses données
/// ont été converties au nouveau schéma.
/// </param>
/// <param name="TablesNonReprises">
/// Tables des modules retirés présentes dans le fichier mais pas remises en
/// place. Elles restent lisibles dans le fichier de sauvegarde.
/// </param>
public record RestaurationDto(
    string NomFichier,
    DateTime DateSauvegarde,
    int NombreTables,
    int NombreLignes,
    bool AncienneVersion = false,
    int TablesNonReprises = 0);
