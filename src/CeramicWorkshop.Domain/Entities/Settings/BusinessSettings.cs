using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Entities.Settings;

/// <summary>
/// Identité de l'atelier et paramètres régionaux. Enregistrement unique (Id = 1).
/// Les valeurs par défaut correspondent à une entreprise algérienne.
/// </summary>
public class BusinessSettings : AuditableEntity
{
    public string WorkshopName { get; set; } = "CERAMIPRO";
    public string? LegalName { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Email { get; set; }
    public string? Website { get; set; }
    public string? LogoPath { get; set; }

    /// <summary>Numéro d'identification fiscale.</summary>
    public string? TaxIdentificationNumber { get; set; }

    /// <summary>Registre du commerce.</summary>
    public string? TradeRegisterNumber { get; set; }

    /// <summary>Article d'imposition.</summary>
    public string? TaxArticleNumber { get; set; }

    // Paramètres régionaux : modifiables pour préparer d'autres langues et devises.
    public string CurrencyCode { get; set; } = "DZD";
    public string CurrencySymbol { get; set; } = "DA";
    public string Culture { get; set; } = "fr-DZ";
    public string TimeZoneId { get; set; } = "Africa/Algiers";
    public string DateFormat { get; set; } = "dd/MM/yyyy";
    public int CurrencyDecimals { get; set; } = 2;

    public decimal DefaultTaxRate { get; set; } = 19m;
    public bool TaxEnabled { get; set; }

    public string InvoicePrefix { get; set; } = "FAC";
    public string SalePrefix { get; set; } = "VTE";
    public string PurchasePrefix { get; set; } = "ACH";
    public string ProductionPrefix { get; set; } = "PROD";
    public string CustomOrderPrefix { get; set; } = "CMD";
    public string PaymentPrefix { get; set; } = "PAI";
    public string FiringPrefix { get; set; } = "CUIS";

    public string? InvoiceFooter { get; set; }
}
