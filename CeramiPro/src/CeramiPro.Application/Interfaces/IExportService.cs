using CeramiPro.Application.DTOs.Finances;

namespace CeramiPro.Application.Interfaces;

/// <summary>Format demandé pour un export.</summary>
public enum FormatExport
{
    Excel,
    Pdf
}

/// <summary>
/// Export des rapports et des listes vers un tableur et vers un document
/// imprimable.
/// </summary>
public interface IExportService
{
    /// <summary>Classeur Excel, avec en-têtes, largeurs et totaux mis en forme.</summary>
    Task<(string NomFichier, byte[] Contenu)> ExcelAsync(
        RapportRequete requete, CancellationToken cancellationToken = default);

    /// <summary>Rapport au format PDF, prêt à imprimer.</summary>
    Task<(string NomFichier, byte[] Contenu)> PdfAsync(
        RapportRequete requete, CancellationToken cancellationToken = default);

    /// <summary>
    /// Export d'un tableau quelconque : c'est ce qui permet à n'importe quel
    /// écran de liste d'offrir le même bouton « Exporter », sans code
    /// particulier par module.
    /// </summary>
    Task<(string NomFichier, byte[] Contenu)> TableauAsync(
        string titre,
        IReadOnlyList<string> colonnes,
        IReadOnlyList<IReadOnlyList<string>> lignes,
        FormatExport format,
        CancellationToken cancellationToken = default);
}
