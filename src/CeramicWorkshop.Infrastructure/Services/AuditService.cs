using System.Text.Json;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Entities.Audit;
using CeramicWorkshop.Domain.Enums;
using CeramicWorkshop.Infrastructure.Data;
using Microsoft.Extensions.Logging;

namespace CeramicWorkshop.Infrastructure.Services;

/// <summary>
/// Écrit le journal des opérations importantes (règle métier n°20).
/// L'écriture est immédiate afin que la trace subsiste même si l'opération
/// principale échoue par la suite.
/// </summary>
public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly ILogger<AuditService> _journal;

    private static readonly JsonSerializerOptions OptionsJson = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public AuditService(
        ApplicationDbContext context,
        ICurrentUserService utilisateurCourant,
        IDateTimeService horloge,
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
            UserId = _utilisateurCourant.UserId,
            UserName = _utilisateurCourant.UserName,
            Action = action,
            EntityName = nomEntite,
            EntityId = identifiantEntite,
            Description = description,
            IpAddress = _utilisateurCourant.IpAddress,
            OccurredAt = _horloge.UtcNow,
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
