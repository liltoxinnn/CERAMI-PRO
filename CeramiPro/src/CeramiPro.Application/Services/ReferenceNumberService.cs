using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Numérote les documents de façon lisible et continue : « ACH-2026-0007 ».
/// Le numéro le plus élevé de l'année en cours sert de point de départ, ce qui
/// évite les trous après une suppression et reste compréhensible par l'atelier.
/// </summary>
public class ReferenceNumberService : IReferenceNumberService
{
    private const int LongueurCompteur = 4;

    private readonly IApplicationDbContext _context;
    private readonly IServiceDateHeure _horloge;

    public ReferenceNumberService(IApplicationDbContext context, IServiceDateHeure horloge)
    {
        _context = context;
        _horloge = horloge;
    }

    public async Task<string> GenererAsync(TypeDocument type, CancellationToken cancellationToken = default)
    {
        var parametres = await _context.BusinessSettings.OrderBy(p => p.Id).FirstOrDefaultAsync(cancellationToken);
        var annee = _horloge.Aujourdhui.Year;

        var prefixe = type switch
        {
            TypeDocument.Client => parametres?.CustomerPrefix ?? "CLI",
            TypeDocument.Produit => parametres?.ProductPrefix ?? "PRD",
            TypeDocument.Commande => parametres?.CustomOrderPrefix ?? "CMD",
            TypeDocument.Vente => parametres?.SalePrefix ?? "VTE",
            TypeDocument.Paiement => parametres?.PaymentPrefix ?? "PAI",
            TypeDocument.Depense => parametres?.ExpensePrefix ?? "DEP",
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

        TypeDocument.Produit => await _context.Products
            .IgnoreQueryFilters().Where(p => p.Reference.StartsWith(debut))
            .Select(p => p.Reference).ToListAsync(cancellationToken),

        TypeDocument.Commande => await _context.CustomOrders
            .IgnoreQueryFilters().Where(c => c.OrderNumber.StartsWith(debut))
            .Select(c => c.OrderNumber).ToListAsync(cancellationToken),

        TypeDocument.Vente => await _context.Sales
            .IgnoreQueryFilters().Where(v => v.SaleNumber.StartsWith(debut))
            .Select(v => v.SaleNumber).ToListAsync(cancellationToken),

        TypeDocument.Paiement => await _context.Payments
            .IgnoreQueryFilters().Where(p => p.PaymentNumber.StartsWith(debut))
            .Select(p => p.PaymentNumber).ToListAsync(cancellationToken),

        _ => await _context.Expenses
            .IgnoreQueryFilters().Where(d => d.Reference.StartsWith(debut))
            .Select(d => d.Reference).ToListAsync(cancellationToken)
    };
}
