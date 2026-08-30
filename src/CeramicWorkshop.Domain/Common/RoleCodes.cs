namespace CeramicWorkshop.Domain.Common;

/// <summary>Codes des rôles livrés avec le logiciel.</summary>
public static class RoleCodes
{
    public const string Administrateur = "administrateur";
    public const string Responsable = "responsable";
    public const string Employe = "employe";
    public const string Caissier = "caissier";

    public static readonly IReadOnlyList<(string Code, string Nom, string Description)> Catalogue = new[]
    {
        (Administrateur, "Administrateur", "Accès complet à toutes les fonctions du logiciel."),
        (Responsable, "Responsable", "Ventes, production, stock, clients, fournisseurs et rapports."),
        (Employe, "Employé", "Production, étapes de fabrication et tâches assignées."),
        (Caissier, "Caissier", "Ventes, factures et paiements.")
    };
}
