using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Settings;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Settings;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Paramètres de l'atelier : identité, coordonnées, devise et préfixes de numérotation.
/// Un seul enregistrement existe ; il est créé automatiquement à l'installation.
/// </summary>
public class ParametresService : IParametresService
{
    private readonly IApplicationDbContext _context;
    private readonly IAuditService _audit;

    public ParametresService(IApplicationDbContext context, IAuditService audit)
    {
        _context = context;
        _audit = audit;
    }

    public async Task<ParametresAtelierDto> ObtenirAsync(CancellationToken cancellationToken = default)
    {
        var parametres = await ChargerAsync(cancellationToken);
        return Convertir(parametres);
    }

    public async Task<ParametresAtelierDto> ModifierAsync(ParametresAtelierDto requete, CancellationToken cancellationToken = default)
    {
        var parametres = await ChargerAsync(cancellationToken);

        parametres.WorkshopName = requete.NomAtelier.Trim();
        parametres.LegalName = Nettoyer(requete.RaisonSociale);
        parametres.Address = Nettoyer(requete.Adresse);
        parametres.City = Nettoyer(requete.Ville);
        parametres.PhoneNumber = Nettoyer(requete.Telephone);
        parametres.Email = Nettoyer(requete.Email);
        parametres.Website = Nettoyer(requete.SiteWeb);
        parametres.LogoPath = Nettoyer(requete.CheminLogo);

        parametres.TaxIdentificationNumber = Nettoyer(requete.NumeroIdentificationFiscale);
        parametres.TradeRegisterNumber = Nettoyer(requete.RegistreCommerce);
        parametres.TaxArticleNumber = Nettoyer(requete.ArticleImposition);

        parametres.CurrencyCode = requete.CodeDevise.Trim().ToUpperInvariant();
        parametres.CurrencySymbol = requete.SymboleDevise.Trim();
        parametres.Culture = requete.Culture.Trim();
        parametres.TimeZoneId = requete.FuseauHoraire.Trim();
        parametres.DateFormat = requete.FormatDate.Trim();
        parametres.CurrencyDecimals = requete.DecimalesDevise;

        parametres.DefaultTaxRate = requete.TauxTvaParDefaut;
        parametres.TaxEnabled = requete.TvaActivee;

        parametres.CustomerPrefix = requete.PrefixeClient.Trim().ToUpperInvariant();
        parametres.ProductPrefix = requete.PrefixeProduit.Trim().ToUpperInvariant();
        parametres.CustomOrderPrefix = requete.PrefixeCommande.Trim().ToUpperInvariant();
        parametres.SalePrefix = requete.PrefixeVente.Trim().ToUpperInvariant();
        parametres.PaymentPrefix = requete.PrefixePaiement.Trim().ToUpperInvariant();
        parametres.ExpensePrefix = requete.PrefixeDepense.Trim().ToUpperInvariant();

        parametres.ReceiptFooter = Nettoyer(requete.PiedDePageRecu);

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(BusinessSettings), parametres.Id.ToString(),
            "Modification des paramètres de l'atelier.", null, cancellationToken);

        return Convertir(parametres);
    }

    private async Task<BusinessSettings> ChargerAsync(CancellationToken cancellationToken)
    {
        var parametres = await _context.BusinessSettings.OrderBy(p => p.Id).FirstOrDefaultAsync(cancellationToken);

        if (parametres is null)
        {
            throw new IntrouvableException(
                "Les paramètres de l'atelier sont introuvables. Relancez l'initialisation de la base de données.");
        }

        return parametres;
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static ParametresAtelierDto Convertir(BusinessSettings p) => new()
    {
        NomAtelier = p.WorkshopName,
        RaisonSociale = p.LegalName,
        Adresse = p.Address,
        Ville = p.City,
        Telephone = p.PhoneNumber,
        Email = p.Email,
        SiteWeb = p.Website,
        CheminLogo = p.LogoPath,
        NumeroIdentificationFiscale = p.TaxIdentificationNumber,
        RegistreCommerce = p.TradeRegisterNumber,
        ArticleImposition = p.TaxArticleNumber,
        CodeDevise = p.CurrencyCode,
        SymboleDevise = p.CurrencySymbol,
        Culture = p.Culture,
        FuseauHoraire = p.TimeZoneId,
        FormatDate = p.DateFormat,
        DecimalesDevise = p.CurrencyDecimals,
        TauxTvaParDefaut = p.DefaultTaxRate,
        TvaActivee = p.TaxEnabled,
        PrefixeClient = p.CustomerPrefix,
        PrefixeProduit = p.ProductPrefix,
        PrefixeCommande = p.CustomOrderPrefix,
        PrefixeVente = p.SalePrefix,
        PrefixePaiement = p.PaymentPrefix,
        PrefixeDepense = p.ExpensePrefix,
        PiedDePageRecu = p.ReceiptFooter
    };
}
