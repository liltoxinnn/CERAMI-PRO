using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CeramicWorkshop.Infrastructure.Data;

/// <summary>
/// PostgreSQL enregistre les dates en « timestamp with time zone ».
/// Ce convertisseur garantit que toute date écrite est en UTC et que toute date lue
/// est bien marquée comme UTC, quelle que soit la saisie effectuée dans l'interface.
/// </summary>
public class UtcDateTimeConverter : ValueConverter<DateTime, DateTime>
{
    public UtcDateTimeConverter()
        : base(
            valeur => Normaliser(valeur),
            valeur => DateTime.SpecifyKind(valeur, DateTimeKind.Utc))
    {
    }

    private static DateTime Normaliser(DateTime valeur) => valeur.Kind switch
    {
        DateTimeKind.Utc => valeur,
        DateTimeKind.Local => valeur.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valeur, DateTimeKind.Utc)
    };
}

/// <summary>Version acceptant les dates facultatives.</summary>
public class NullableUtcDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public NullableUtcDateTimeConverter()
        : base(
            valeur => valeur.HasValue ? Normaliser(valeur.Value) : null,
            valeur => valeur.HasValue ? DateTime.SpecifyKind(valeur.Value, DateTimeKind.Utc) : null)
    {
    }

    private static DateTime Normaliser(DateTime valeur) => valeur.Kind switch
    {
        DateTimeKind.Utc => valeur,
        DateTimeKind.Local => valeur.ToUniversalTime(),
        _ => DateTime.SpecifyKind(valeur, DateTimeKind.Utc)
    };
}
