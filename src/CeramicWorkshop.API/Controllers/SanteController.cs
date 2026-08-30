using CeramicWorkshop.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.API.Controllers;

/// <summary>État de fonctionnement du serveur et de la base de données.</summary>
[ApiController]
[Route("api/sante")]
[AllowAnonymous]
public class SanteController : ControllerBase
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<SanteController> _journal;

    public SanteController(ApplicationDbContext context, ILogger<SanteController> journal)
    {
        _context = context;
        _journal = journal;
    }

    /// <summary>Vérifie que l'API répond et que la base de données est accessible.</summary>
    [HttpGet]
    public async Task<IActionResult> Etat(CancellationToken cancellationToken)
    {
        var baseAccessible = false;
        var migrationsEnAttente = 0;

        try
        {
            baseAccessible = await _context.Database.CanConnectAsync(cancellationToken);

            if (baseAccessible)
            {
                migrationsEnAttente = (await _context.Database.GetPendingMigrationsAsync(cancellationToken)).Count();
            }
        }
        catch (Exception ex)
        {
            _journal.LogError(ex, "La base de données n'est pas joignable.");
        }

        var version = typeof(SanteController).Assembly.GetName().Version?.ToString(3) ?? "1.0.0";

        return Ok(new
        {
            application = "CERAMIPRO",
            version,
            serveur = "Opérationnel",
            baseDeDonnees = baseAccessible ? "Connectée" : "Non joignable",
            migrationsEnAttente,
            horodatage = DateTime.UtcNow
        });
    }
}
