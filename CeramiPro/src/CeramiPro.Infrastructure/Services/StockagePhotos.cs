using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
using Microsoft.Extensions.Configuration;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Photos rangées dans un dossier de l'ordinateur — par défaut
/// « %LOCALAPPDATA%\CeramiPro\photos », modifiable par « Photos:Dossier »,
/// par exemple pour un dossier partagé entre plusieurs postes.
/// </summary>
public class StockagePhotos : IStockagePhotos
{
    public const string CleDossier = "Photos:Dossier";

    /// <summary>Une photo de téléphone dépasse rarement 15 Mo ; au-delà, c'est autre chose.</summary>
    public const long TailleMaximale = 25 * 1024 * 1024;

    private static readonly string[] Extensions = { ".jpg", ".jpeg", ".png", ".webp", ".bmp", ".gif" };

    public StockagePhotos(IConfiguration configuration)
        => Racine = Path.GetFullPath(configuration[CleDossier]
            ?? Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CeramiPro", "photos"));

    public string Racine { get; }

    public IReadOnlyCollection<string> ExtensionsAcceptees => Extensions;

    public async Task<string> ImporterAsync(
        string cheminSource, string sousDossier, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(cheminSource))
        {
            throw new RegleMetierException($"Le fichier « {Path.GetFileName(cheminSource)} » est introuvable.");
        }

        var extension = Path.GetExtension(cheminSource).ToLowerInvariant();

        if (!Extensions.Contains(extension))
        {
            throw new RegleMetierException(
                $"« {Path.GetFileName(cheminSource)} » n'est pas une photo : formats acceptés " +
                string.Join(", ", Extensions.Select(e => e.TrimStart('.').ToUpperInvariant())) + ".");
        }

        if (new FileInfo(cheminSource).Length > TailleMaximale)
        {
            throw new RegleMetierException(
                $"« {Path.GetFileName(cheminSource)} » dépasse 25 Mo : réduisez-la avant de l'ajouter.");
        }

        var dossierRelatif = Nettoyer(sousDossier);
        var nomFichier = $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Guid.NewGuid():N}"[..28] + extension;
        var relatif = Path.Combine(dossierRelatif, nomFichier);
        var destination = Path.Combine(Racine, relatif);

        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);

        await using (var source = File.OpenRead(cheminSource))
        await using (var cible = File.Create(destination))
        {
            await source.CopyToAsync(cible, cancellationToken);
        }

        // En base, toujours avec des barres obliques : le chemin reste le même
        // quel que soit l'ordinateur qui le relit.
        return relatif.Replace('\\', '/');
    }

    public string CheminComplet(string chemin)
        => Path.IsPathRooted(chemin)
            ? chemin
            : Path.GetFullPath(Path.Combine(Racine, chemin.Replace('/', Path.DirectorySeparatorChar)));

    public void Supprimer(string chemin)
    {
        if (string.IsNullOrWhiteSpace(chemin))
        {
            return;
        }

        var complet = CheminComplet(chemin);

        // Jamais en dehors du dossier des photos : une ancienne fiche peut
        // pointer vers un fichier personnel, qui n'appartient pas au logiciel.
        if (!complet.StartsWith(Racine + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        try
        {
            File.Delete(complet);
        }
        catch (IOException)
        {
            // Une photo ouverte dans une visionneuse sera effacée à la prochaine fois.
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    /// <summary>« produits/12 » : lettres, chiffres et barres seulement.</summary>
    private static string Nettoyer(string sousDossier)
    {
        var parties = sousDossier
            .Replace('\\', '/')
            .Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p != "." && p != "..")
            .Select(p => new string(p.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray()))
            .Where(p => p.Length > 0);

        return Path.Combine(parties.ToArray());
    }
}
