using CeramiPro.Domain.Enums;

namespace CeramiPro.Application.DTOs.Alertes;

/// <summary>Une alerte affichée dans le centre de notifications.</summary>
public record AlerteDto(
    int Id,
    NotificationType Type,
    string TypeLibelle,
    NotificationSeverity Gravite,
    string GraviteLibelle,
    string Titre,
    string Message,
    string? Adresse,
    bool Lue,
    DateTime Date);

/// <summary>Compteurs affichés dans l'en-tête et sur le tableau de bord.</summary>
public record ResumeAlertesDto(int Total, int NonLues, int Critiques);

/// <summary>Réglage d'une alerte, modifiable par le responsable.</summary>
public class ReglageAlerteDto
{
    public int Id { get; set; }
    public NotificationType Type { get; set; }
    public string TypeLibelle { get; set; } = string.Empty;
    public string Explication { get; set; } = string.Empty;
    public bool Active { get; set; }
    public int? SeuilJours { get; set; }
    public decimal? SeuilValeur { get; set; }
    public bool AttendDesJours { get; set; }
}

public class FiltreAlertesRequete
{
    public bool SeulementNonLues { get; set; }
    public NotificationSeverity? Gravite { get; set; }
}
