using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Numérote les documents de façon lisible et continue : « ACH-2026-0007 ».
/// Le numéro le plus élevé de l'année en cours sert de point de départ, ce qui
/// évite les trous après une suppression et reste compréhensible par l'atelier.
/// </summary>
public class ReferenceNumberService : IReferenceNumberService
{
    private const int LongueurCompteur = 4;

    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _horloge;

    public ReferenceNumberService(IApplicationDbContext context, IDateTimeService horloge)
    {
        _context = context;
        _horloge = horloge;
    }

    public async Task<string> GenererAsync(TypeDocument type, CancellationToken cancellationToken = default)
    {
        var parametres = await _context.BusinessSettings.OrderBy(p => p.Id).FirstOrDefaultAsync(cancellationToken);
        var annee = _horloge.AujourdHui.Year;

        var prefixe = type switch
        {
            TypeDocument.Client => "CLI",
            TypeDocument.Fournisseur => "FRN",
            TypeDocument.Matiere => "MAT",
            TypeDocument.LotMatiere => "LOT",
            TypeDocument.Produit => "PRD",
            TypeDocument.Achat => parametres?.PurchasePrefix ?? "ACH",
            TypeDocument.ReglementFournisseur => "RGF",
            TypeDocument.Production => parametres?.ProductionPrefix ?? "PROD",
            TypeDocument.Cuisson => parametres?.FiringPrefix ?? "CUIS",
            TypeDocument.Decoration => "DEC",
            TypeDocument.Qualite => "QUA",
            TypeDocument.Commande => parametres?.CustomOrderPrefix ?? "CMD",
            TypeDocument.Vente => parametres?.SalePrefix ?? "VTE",
            TypeDocument.Facture => parametres?.InvoicePrefix ?? "FAC",
            TypeDocument.Paiement => parametres?.PaymentPrefix ?? "PAI",
            TypeDocument.Depense => "DEP",
            TypeDocument.Ajustement => "AJU",
            _ => "DOC"
        };

        var debut = $"{prefixe}-{annee}-";
        var numeros = await NumerosExistantsAsync(type, debut, cancellationToken);

        var dernier = numeros
            .Select(n => int.TryParse(n[debut.Length..], out var valeur) ? valeur : 0)
            .DefaultIfEmpty(0)
            .Max();

        return debut + (dernier + 1).ToString(new string('0', LongueurCompteur));
    }

    /// <summary>Numéros déjà attribués cette année pour ce type de document.</summary>
    private async Task<List<string>> NumerosExistantsAsync(
        TypeDocument type, string debut, CancellationToken cancellationToken) => type switch
    {
        TypeDocument.Client => await _context.Customers
            .IgnoreQueryFilters().Where(c => c.CustomerNumber.StartsWith(debut))
            .Select(c => c.CustomerNumber).ToListAsync(cancellationToken),

        TypeDocument.Fournisseur => await _context.Suppliers
            .IgnoreQueryFilters().Where(f => f.SupplierNumber.StartsWith(debut))
            .Select(f => f.SupplierNumber).ToListAsync(cancellationToken),

        TypeDocument.Matiere => await _context.Materials
            .IgnoreQueryFilters().Where(m => m.Reference.StartsWith(debut))
            .Select(m => m.Reference).ToListAsync(cancellationToken),

        TypeDocument.LotMatiere => await _context.MaterialBatches
            .IgnoreQueryFilters().Where(l => l.BatchNumber.StartsWith(debut))
            .Select(l => l.BatchNumber).ToListAsync(cancellationToken),

        TypeDocument.Produit => await _context.Products
            .IgnoreQueryFilters().Where(p => p.Reference.StartsWith(debut))
            .Select(p => p.Reference).ToListAsync(cancellationToken),

        TypeDocument.Achat => await _context.Purchases
            .IgnoreQueryFilters().Where(a => a.PurchaseNumber.StartsWith(debut))
            .Select(a => a.PurchaseNumber).ToListAsync(cancellationToken),

        TypeDocument.ReglementFournisseur => await _context.SupplierPayments
            .IgnoreQueryFilters().Where(r => r.PaymentNumber.StartsWith(debut))
            .Select(r => r.PaymentNumber).ToListAsync(cancellationToken),

        TypeDocument.Production => await _context.ProductionOrders
            .IgnoreQueryFilters().Where(o => o.ProductionNumber.StartsWith(debut))
            .Select(o => o.ProductionNumber).ToListAsync(cancellationToken),

        TypeDocument.Cuisson => await _context.FiringBatches
            .IgnoreQueryFilters().Where(c => c.BatchNumber.StartsWith(debut))
            .Select(c => c.BatchNumber).ToListAsync(cancellationToken),

        TypeDocument.Decoration => await _context.DecorationOrders
            .IgnoreQueryFilters().Where(d => d.Reference.StartsWith(debut))
            .Select(d => d.Reference).ToListAsync(cancellationToken),

        TypeDocument.Qualite => await _context.QualityChecks
            .IgnoreQueryFilters().Where(q => q.Reference.StartsWith(debut))
            .Select(q => q.Reference).ToListAsync(cancellationToken),

        TypeDocument.Commande => await _context.CustomOrders
            .IgnoreQueryFilters().Where(c => c.OrderNumber.StartsWith(debut))
            .Select(c => c.OrderNumber).ToListAsync(cancellationToken),

        TypeDocument.Vente => await _context.Sales
            .IgnoreQueryFilters().Where(v => v.SaleNumber.StartsWith(debut))
            .Select(v => v.SaleNumber).ToListAsync(cancellationToken),

        TypeDocument.Facture => await _context.Invoices
            .IgnoreQueryFilters().Where(f => f.InvoiceNumber.StartsWith(debut))
            .Select(f => f.InvoiceNumber).ToListAsync(cancellationToken),

        TypeDocument.Paiement => await _context.Payments
            .IgnoreQueryFilters().Where(p => p.PaymentNumber.StartsWith(debut))
            .Select(p => p.PaymentNumber).ToListAsync(cancellationToken),

        TypeDocument.Depense => await _context.Expenses
            .IgnoreQueryFilters().Where(d => d.Reference.StartsWith(debut))
            .Select(d => d.Reference).ToListAsync(cancellationToken),

        TypeDocument.Ajustement => await _context.StockAdjustments
            .IgnoreQueryFilters().Where(a => a.Reference.StartsWith(debut))
            .Select(a => a.Reference).ToListAsync(cancellationToken),

        _ => new List<string>()
    };
}
