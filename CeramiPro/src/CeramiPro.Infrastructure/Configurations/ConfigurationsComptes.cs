using CeramiPro.Domain.Entities.Audit;
using CeramiPro.Domain.Entities.Identity;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CeramiPro.Infrastructure.Configurations;

/// <summary>Comptes du logiciel. Le rôle est une valeur, pas une table.</summary>
public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.Property(u => u.UserName).HasMaxLength(60).IsRequired();
        builder.Property(u => u.FullName).HasMaxLength(150).IsRequired();
        builder.Property(u => u.Email).HasMaxLength(150);
        builder.Property(u => u.PhoneNumber).HasMaxLength(30);
        builder.Property(u => u.PasswordHash).HasMaxLength(512).IsRequired();
        builder.Property(u => u.Role).HasConversion<int>().IsRequired().HasDefaultValue(RoleUtilisateur.Employe);

        builder.HasIndex(u => u.UserName).IsUnique();
    }
}

/// <summary>Journal des opérations importantes.</summary>
public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.Property(a => a.UserName).HasMaxLength(60);
        builder.Property(a => a.Action).HasConversion<int>();
        builder.Property(a => a.EntityName).HasMaxLength(80).IsRequired();
        builder.Property(a => a.EntityId).HasMaxLength(40);
        builder.Property(a => a.Changes).HasColumnType("jsonb");
        builder.Property(a => a.Description).HasMaxLength(1000);
        builder.Property(a => a.Workstation).HasMaxLength(60);

        builder.HasIndex(a => a.OccurredAt);
        builder.HasIndex(a => new { a.EntityName, a.EntityId });

        builder.HasOne(a => a.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

/// <summary>Identité de l'atelier : un seul enregistrement.</summary>
public class BusinessSettingsConfiguration : IEntityTypeConfiguration<BusinessSettings>
{
    public void Configure(EntityTypeBuilder<BusinessSettings> builder)
    {
        builder.ToTable("BusinessSettings");

        builder.Property(s => s.WorkshopName).HasMaxLength(150).IsRequired();
        builder.Property(s => s.LegalName).HasMaxLength(150);
        builder.Property(s => s.Address).HasMaxLength(300);
        builder.Property(s => s.City).HasMaxLength(100);
        builder.Property(s => s.PhoneNumber).HasMaxLength(30);
        builder.Property(s => s.Email).HasMaxLength(150);
        builder.Property(s => s.Website).HasMaxLength(150);
        builder.Property(s => s.LogoPath).HasMaxLength(400);

        builder.Property(s => s.TaxIdentificationNumber).HasMaxLength(40);
        builder.Property(s => s.TradeRegisterNumber).HasMaxLength(40);
        builder.Property(s => s.TaxArticleNumber).HasMaxLength(40);

        builder.Property(s => s.CurrencyCode).HasMaxLength(3).IsRequired();
        builder.Property(s => s.CurrencySymbol).HasMaxLength(10).IsRequired();
        builder.Property(s => s.Culture).HasMaxLength(10).IsRequired();
        builder.Property(s => s.TimeZoneId).HasMaxLength(60).IsRequired();
        builder.Property(s => s.DateFormat).HasMaxLength(20).IsRequired();
        builder.Property(s => s.DefaultTaxRate).HasPrecision(5, 2);

        // Les trois préfixes ajoutés lors de la simplification ont une valeur
        // par défaut en base : une sauvegarde de l'ancienne version, qui ne
        // les connaît pas, se restaure ainsi sans trou.
        builder.Property(s => s.CustomerPrefix).HasMaxLength(10).IsRequired().HasDefaultValue("CLI");
        builder.Property(s => s.ProductPrefix).HasMaxLength(10).IsRequired().HasDefaultValue("PRD");
        builder.Property(s => s.CustomOrderPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.SalePrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.PaymentPrefix).HasMaxLength(10).IsRequired();
        builder.Property(s => s.ExpensePrefix).HasMaxLength(10).IsRequired().HasDefaultValue("DEP");
        builder.Property(s => s.ReceiptFooter).HasMaxLength(1000);
    }
}

/// <summary>Réglages techniques clé/valeur.</summary>
public class SystemSettingConfiguration : IEntityTypeConfiguration<SystemSetting>
{
    public void Configure(EntityTypeBuilder<SystemSetting> builder)
    {
        builder.ToTable("SystemSettings");

        builder.Property(s => s.Key).HasMaxLength(100).IsRequired();
        builder.Property(s => s.Value).HasMaxLength(1000);
        builder.Property(s => s.Category).HasMaxLength(60).IsRequired();
        builder.Property(s => s.ValueType).HasMaxLength(20).IsRequired();
        builder.Property(s => s.Description).HasMaxLength(500);

        builder.HasIndex(s => s.Key).IsUnique();
    }
}
