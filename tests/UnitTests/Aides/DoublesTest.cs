using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Enums;

namespace CeramicWorkshop.UnitTests.Aides;

/// <summary>Utilisateur simulé pour les tests.</summary>
public class UtilisateurCourantFactice : ICurrentUserService
{
    public int? UserId { get; set; }
    public string? UserName { get; set; }
    public string? RoleCode { get; set; }
    public string? IpAddress { get; set; } = "127.0.0.1";
    public bool EstAuthentifie => UserId is not null;

    public HashSet<string> Droits { get; } = new();

    public bool PossedeDroit(string codeDroit) => Droits.Contains(codeDroit);
}

/// <summary>Horloge fixe : les tests ne dépendent pas de l'heure réelle.</summary>
public class HorlogeFactice : IDateTimeService
{
    public HorlogeFactice(DateTime? depart = null)
        => UtcNow = depart ?? new DateTime(2026, 3, 15, 9, 0, 0, DateTimeKind.Utc);

    public DateTime UtcNow { get; set; }

    public DateTime MaintenantAtelier => UtcNow.AddHours(1);

    public DateTime AujourdHui => MaintenantAtelier.Date;

    public DateTime VersHeureAtelier(DateTime utc) => utc.AddHours(1);

    public DateTime VersUtc(DateTime heureAtelier) => heureAtelier.AddHours(-1);

    public void Avancer(TimeSpan duree) => UtcNow = UtcNow.Add(duree);
}

/// <summary>Journal d'audit simulé : conserve les opérations enregistrées.</summary>
public class AuditFactice : IAuditService
{
    public List<(AuditAction Action, string Entite, string? Identifiant, string? Description)> Traces { get; } = new();

    public Task EnregistrerAsync(
        AuditAction action,
        string nomEntite,
        string? identifiantEntite = null,
        string? description = null,
        object? changements = null,
        CancellationToken cancellationToken = default)
    {
        Traces.Add((action, nomEntite, identifiantEntite, description));
        return Task.CompletedTask;
    }
}

/// <summary>Générateur de jetons simulé.</summary>
public class JetonsFactices : ITokenService
{
    public (string Jeton, DateTime Expiration) CreerJetonAcces(
        CeramicWorkshop.Domain.Entities.Identity.User utilisateur, IReadOnlyList<string> droits)
        => ($"jeton-{utilisateur.UserName}", DateTime.UtcNow.AddHours(2));

    public string CreerJetonRenouvellement() => Guid.NewGuid().ToString("N");
}
