using CeramicWorkshop.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CeramicWorkshop.API.Controllers;

/// <summary>
/// Dépôt des photos et justificatifs. Le nom d'origine du fichier n'est jamais
/// réutilisé : le serveur génère lui-même un nom, ce qui évite qu'un fichier
/// déposé puisse écraser un autre ou être exécuté.
/// </summary>
[ApiController]
[Route("api/fichiers")]
[Authorize]
public class FichiersController : ControllerBase
{
    /// <summary>Taille maximale acceptée, en octets (8 Mo).</summary>
    public const long TailleMaximale = 8 * 1024 * 1024;

    /// <summary>Dossier public dans lequel les fichiers sont enregistrés.</summary>
    public const string DossierPublic = "fichiers";

    private static readonly Dictionary<string, string> TypesAutorises = new(StringComparer.OrdinalIgnoreCase)
    {
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png",
        [".webp"] = "image/webp",
        [".pdf"] = "application/pdf"
    };

    /// <summary>
    /// Premiers octets attendus pour chaque format. Un fichier renommé en
    /// « .png » mais qui n'en est pas un est ainsi refusé, quelle que soit
    /// l'extension annoncée.
    /// </summary>
    private static readonly Dictionary<string, byte[][]> SignaturesAttendues =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".jpeg"] = new[] { new byte[] { 0xFF, 0xD8, 0xFF } },
            [".png"] = new[] { new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A } },
            [".webp"] = new[] { "RIFF"u8.ToArray() },
            [".pdf"] = new[] { "%PDF"u8.ToArray() }
        };

    private readonly IWebHostEnvironment _environnement;
    private readonly ILogger<FichiersController> _journal;

    public FichiersController(IWebHostEnvironment environnement, ILogger<FichiersController> journal)
    {
        _environnement = environnement;
        _journal = journal;
    }

    /// <summary>Enregistre un fichier et renvoie son adresse.</summary>
    [HttpPost]
    [RequestSizeLimit(TailleMaximale)]
    public async Task<IActionResult> Deposer(IFormFile fichier, CancellationToken cancellationToken)
    {
        if (fichier is null || fichier.Length == 0)
        {
            return BadRequest(new ErreurApi { Message = "Aucun fichier n'a été envoyé." });
        }

        if (fichier.Length > TailleMaximale)
        {
            return BadRequest(new ErreurApi
            {
                Message = "Le fichier dépasse la taille autorisée de 8 Mo. Réduisez-le puis réessayez."
            });
        }

        var extension = Path.GetExtension(fichier.FileName);

        if (string.IsNullOrWhiteSpace(extension) || !TypesAutorises.TryGetValue(extension, out var typeAttendu))
        {
            return BadRequest(new ErreurApi
            {
                Message = "Format non accepté. Utilisez une image JPG, PNG ou WEBP, ou un document PDF."
            });
        }

        if (!string.Equals(fichier.ContentType, typeAttendu, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest(new ErreurApi
            {
                Message = "Le contenu du fichier ne correspond pas à son extension."
            });
        }

        if (!await SignatureValideAsync(fichier, extension, cancellationToken))
        {
            return BadRequest(new ErreurApi
            {
                Message = "Ce fichier n'est pas une image ou un PDF valide."
            });
        }

        var racine = string.IsNullOrWhiteSpace(_environnement.WebRootPath)
            ? Path.Combine(_environnement.ContentRootPath, "wwwroot")
            : _environnement.WebRootPath;

        var sousDossier = DateTime.UtcNow.ToString("yyyy-MM");
        var dossier = Path.Combine(racine, DossierPublic, sousDossier);
        Directory.CreateDirectory(dossier);

        var nom = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var chemin = Path.Combine(dossier, nom);

        await using (var flux = System.IO.File.Create(chemin))
        {
            await fichier.CopyToAsync(flux, cancellationToken);
        }

        var adresse = $"/{DossierPublic}/{sousDossier}/{nom}";
        _journal.LogInformation("Fichier enregistré : {Adresse}", adresse);

        return Ok(new { chemin = adresse, nomOrigine = Path.GetFileName(fichier.FileName), taille = fichier.Length });
    }

    /// <summary>Vérifie que le contenu correspond réellement au format annoncé.</summary>
    private static async Task<bool> SignatureValideAsync(
        IFormFile fichier, string extension, CancellationToken cancellationToken)
    {
        if (!SignaturesAttendues.TryGetValue(extension, out var signatures))
        {
            return false;
        }

        var longueur = signatures.Max(signature => signature.Length);
        var debut = new byte[longueur];

        await using var flux = fichier.OpenReadStream();
        var lus = await flux.ReadAtLeastAsync(debut, longueur, throwOnEndOfStream: false, cancellationToken);

        return lus >= longueur
               && signatures.Any(signature => debut.AsSpan(0, signature.Length).SequenceEqual(signature));
    }
}
