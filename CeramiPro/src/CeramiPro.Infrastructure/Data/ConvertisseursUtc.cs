using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace CeramiPro.Infrastructure.Data;

/// <summary>
/// Garantit que toute date part en base en temps universel et en revient
/// marquée comme telle. Sans cela, PostgreSQL refuse les dates non qualifiées.
/// </summary>
public class ConvertisseurUtc : ValueConverter<DateTime, DateTime>
{
    public ConvertisseurUtc()
        : base(
            date => date.Kind == DateTimeKind.Utc ? date : date.ToUniversalTime(),
            date => DateTime.SpecifyKind(date, DateTimeKind.Utc))
    {
    }
}

/// <summary>Même conversion, pour les dates facultatives.</summary>
public class ConvertisseurUtcNullable : ValueConverter<DateTime?, DateTime?>
{
    public ConvertisseurUtcNullable()
        : base(
            date => date == null
                ? null
                : (date.Value.Kind == DateTimeKind.Utc ? date : date.Value.ToUniversalTime()),
            date => date == null
                ? null
                : DateTime.SpecifyKind(date.Value, DateTimeKind.Utc))
    {
    }
}
