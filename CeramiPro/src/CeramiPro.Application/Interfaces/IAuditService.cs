using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.Interfaces;

/// <summary>Journalisation des opérations importantes (règle métier n°20).</summary>
public interface IAuditService
{
    Task EnregistrerAsync(
        AuditAction action,
        string nomEntite,
        string? identifiantEntite = null,
        string? description = null,
        object? changements = null,
        CancellationToken cancellationToken = default);
}
