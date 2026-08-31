using System.Text.Json;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Audit;
using CeramiPro.Domain.Enums;
using CeramiPro.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Écrit le journal des opérations importantes (règle métier n°20).
/// L'écriture est immédiate afin que la trace subsiste même si l'opération
/// principale échoue par la suite.
/// </summary>
public class AuditService : IAuditService
{
    private readonly CeramiProDbContext _context;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly ILogger<AuditService> _journal;

    private static readonly JsonSerializerOptions OptionsJson = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AuditService(
        CeramiProDbContext context,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        ILogger<AuditService> journal)
    {
        _context = context;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _journal = journal;
    }

    public async Task EnregistrerAsync(
        AuditAction action,
        string nomEntite,
        string? identifiantEntite = null,
        string? description = null,
        object? changements = null,
        CancellationToken cancellationToken = default)
    {
        var trace = new AuditLog
        {
            UserId = _utilisateurCourant.UtilisateurId,
            UserName = _utilisateurCourant.NomUtilisateur,
            Action = action,
            EntityName = nomEntite,
            EntityId = identifiantEntite,
            Description = description,
            Workstation = Environment.MachineName,
            OccurredAt = _horloge.MaintenantUtc,
            Changes = changements is null ? null : JsonSerializer.Serialize(changements, OptionsJson)
        };

        _context.AuditLogs.Add(trace);

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            // Une trace d'audit ne doit jamais interrompre l'opération de l'utilisateur.
            _context.AuditLogs.Remove(trace);
            _journal.LogError(ex, "Impossible d'enregistrer la trace d'audit « {Action} » sur {Entite}.",
                action, nomEntite);
        }
    }
}
