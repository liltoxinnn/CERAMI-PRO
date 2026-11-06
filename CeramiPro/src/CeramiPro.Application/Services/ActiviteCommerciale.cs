using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// L'activité de l'atelier sur une période, jour par jour : ventes, commandes
/// livrées, argent encaissé et dépenses. Le tableau de bord et les rapports
/// partent des mêmes chiffres, calculés au même endroit : ils ne peuvent pas
/// se contredire.
///
/// Le chiffre d'affaires réunit les ventes au comptoir et les commandes
/// personnalisées livrées. Une commande ne compte qu'à sa livraison : avant,
/// l'argent reçu est un acompte, pas une recette acquise.
/// </summary>
internal sealed class ActiviteCommerciale
{
    public sealed record Vente(DateTime Jour, decimal Total, decimal Tva, decimal Cout);

    public sealed record Livraison(DateTime Jour, decimal Total);

    public sealed record Mouvement(DateTime Jour, decimal Montant);

    private ActiviteCommerciale(
        IReadOnlyList<Vente> ventes,
        IReadOnlyList<Livraison> livraisons,
        IReadOnlyList<Mouvement> encaissements,
        IReadOnlyList<Mouvement> depenses)
    {
        Ventes = ventes;
        Livraisons = livraisons;
        Encaissements = encaissements;
        Depenses = depenses;
    }

    public IReadOnlyList<Vente> Ventes { get; }
    public IReadOnlyList<Livraison> Livraisons { get; }
    public IReadOnlyList<Mouvement> Encaissements { get; }
    public IReadOnlyList<Mouvement> Depenses { get; }

    /// <summary>Lit l'activité de la période ; chaque date est ramenée au jour de l'atelier.</summary>
    public static async Task<ActiviteCommerciale> ChargerAsync(
        IApplicationDbContext context, IServiceDateHeure horloge, PeriodeAtelier periode,
        CancellationToken cancellationToken)
    {
        var (debut, fin) = (periode.DebutUtc, periode.FinUtc);
        DateTime Jour(DateTime utc) => horloge.VersHeureAtelier(utc).Date;

        var ventes = await context.Sales.AsNoTracking()
            .Where(v => v.Status == SaleStatus.Confirmee && v.SaleDate >= debut && v.SaleDate < fin)
            .Select(v => new { v.SaleDate, v.TotalAmount, v.TaxAmount, v.TotalCost })
            .ToListAsync(cancellationToken);

        var livraisons = await context.CustomOrders.AsNoTracking()
            .Where(c => c.Status == CustomOrderStatus.Livree
                        && c.DeliveredAt != null && c.DeliveredAt >= debut && c.DeliveredAt < fin)
            .Select(c => new { DeliveredAt = c.DeliveredAt!.Value, c.TotalAmount })
            .ToListAsync(cancellationToken);

        var encaissements = await context.Payments.AsNoTracking()
            .Where(p => p.PaymentDate >= debut && p.PaymentDate < fin)
            .Select(p => new { p.PaymentDate, p.Amount })
            .ToListAsync(cancellationToken);

        var depenses = await context.Expenses.AsNoTracking()
            .Where(d => d.ExpenseDate >= debut && d.ExpenseDate < fin)
            .Select(d => new { d.ExpenseDate, d.Amount })
            .ToListAsync(cancellationToken);

        return new ActiviteCommerciale(
            ventes.Select(v => new Vente(Jour(v.SaleDate), v.TotalAmount, v.TaxAmount, v.TotalCost)).ToList(),
            livraisons.Select(l => new Livraison(Jour(l.DeliveredAt), l.TotalAmount)).ToList(),
            encaissements.Select(e => new Mouvement(Jour(e.PaymentDate), e.Amount)).ToList(),
            depenses.Select(d => new Mouvement(Jour(d.ExpenseDate), d.Amount)).ToList());
    }

    /// <summary>Totaux des jours retenus.</summary>
    public Bilan Sur(Func<DateTime, bool> jourRetenu)
    {
        var ventes = Ventes.Where(v => jourRetenu(v.Jour)).ToList();
        var livraisons = Livraisons.Where(l => jourRetenu(l.Jour)).ToList();

        return new Bilan(
            ventes.Count,
            ventes.Sum(v => v.Total),
            livraisons.Count,
            livraisons.Sum(l => l.Total),
            ventes.Sum(v => v.Tva),
            ventes.Sum(v => v.Cout),
            Encaissements.Where(e => jourRetenu(e.Jour)).Sum(e => e.Montant),
            Depenses.Where(d => jourRetenu(d.Jour)).Sum(d => d.Montant));
    }

    public Bilan Total => Sur(_ => true);

    public bool EstVide => Ventes.Count == 0 && Livraisons.Count == 0
                           && Encaissements.Count == 0 && Depenses.Count == 0;
}

/// <summary>Chiffres d'une journée, d'un mois ou d'une période.</summary>
internal sealed record Bilan(
    int NombreVentes,
    decimal MontantVentes,
    int NombreLivraisons,
    decimal MontantLivraisons,
    decimal Tva,
    decimal CoutVentes,
    decimal Encaisse,
    decimal Depenses)
{
    /// <summary>Ventes et commandes livrées.</summary>
    public decimal ChiffreAffaires => MontantVentes + MontantLivraisons;

    /// <summary>
    /// Ce que rapporte l'activité avant les charges : le chiffre d'affaires,
    /// sans la TVA collectée ni le coût des pièces vendues.
    /// </summary>
    public decimal Benefice => ChiffreAffaires - Tva - CoutVentes;

    /// <summary>Bénéfice, une fois les dépenses déduites.</summary>
    public decimal Resultat => Benefice - Depenses;

    public bool EstVide => NombreVentes == 0 && NombreLivraisons == 0 && Encaisse == 0 && Depenses == 0;
}
