using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Rassemble en une seule lecture les chiffres du jour, du mois, de la
/// production, des commandes, du stock et des finances, ainsi que les
/// graphiques du tableau de bord.
/// </summary>
public class TableauDeBordService : ITableauDeBordService
{
    /// <summary>Nombre de jours affichés dans le graphique des ventes quotidiennes.</summary>
    public const int JoursGraphique = 30;

    /// <summary>Nombre de mois affichés dans les graphiques mensuels.</summary>
    public const int MoisGraphique = 12;

    /// <summary>Nombre de lignes affichées dans chaque classement.</summary>
    public const int NombreClassement = 10;

    private readonly IApplicationDbContext _context;
    private readonly IServiceDateHeure _horloge;

    public TableauDeBordService(IApplicationDbContext context, IServiceDateHeure horloge)
    {
        _context = context;
        _horloge = horloge;
    }

    public async Task<TableauDeBordDto> ObtenirAsync(CancellationToken cancellationToken = default)
    {
        var maintenant = _horloge.MaintenantUtc;
        var debutJour = DateTime.SpecifyKind(maintenant.Date, DateTimeKind.Utc);
        var finJour = debutJour.AddDays(1);
        var debutMois = new DateTime(maintenant.Year, maintenant.Month, 1, 0, 0, 0, DateTimeKind.Utc);
        var debutGraphiqueJours = debutJour.AddDays(-(JoursGraphique - 1));
        var debutGraphiqueMois = debutMois.AddMonths(-(MoisGraphique - 1));

        var ventes = await _context.Sales
            .AsNoTracking()
            .Where(v => v.Status == SaleStatus.Confirmee && v.SaleDate >= debutGraphiqueMois)
            .Select(v => new LigneVente(
                v.SaleDate, v.TotalAmount, v.TotalAmount - v.TaxAmount - v.TotalCost))
            .ToListAsync(cancellationToken);

        var paiementsDuJour = await _context.Payments
            .AsNoTracking()
            .Where(p => p.PaymentDate >= debutJour && p.PaymentDate < finJour)
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var ventesDuJour = ventes.Where(v => v.Date >= debutJour && v.Date < finJour).ToList();
        var ventesDuMois = ventes.Where(v => v.Date >= debutMois).ToList();

        var depensesDuMois = await _context.Expenses
            .AsNoTracking()
            .Where(d => d.ExpenseDate >= debutMois)
            .SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0m;

        var chiffreMois = ventesDuMois.Sum(v => v.Total);
        var beneficeMois = ventesDuMois.Sum(v => v.Benefice);

        return new TableauDeBordDto(
            new ActiviteDuJourDto(
                ventesDuJour.Sum(v => v.Total),
                ventesDuJour.Sum(v => v.Benefice),
                ventesDuJour.Count,
                paiementsDuJour),
            new ActivitePeriodeDto(
                chiffreMois,
                beneficeMois,
                depensesDuMois,
                beneficeMois - depensesDuMois,
                ventesDuMois.Count),
            await CommandesAsync(maintenant, cancellationToken),
            await StockAsync(cancellationToken),
            await FinancesAsync(depensesDuMois, cancellationToken),
            ConstruireParJour(ventes, debutGraphiqueJours),
            ConstruireParMois(ventes, debutGraphiqueMois, v => v.Total),
            await ProduitsLesPlusVendusAsync(cancellationToken),
            await StockFaibleAsync(cancellationToken));
    }

    private async Task<CommandesResumeDto> CommandesAsync(
        DateTime maintenant, CancellationToken cancellationToken)
    {
        var commandes = await _context.CustomOrders
            .AsNoTracking()
            .Select(c => new { c.Status, c.Deadline })
            .ToListAsync(cancellationToken);

        var actives = commandes
            .Where(c => c.Status != CustomOrderStatus.Livre && c.Status != CustomOrderStatus.Annule)
            .ToList();

        var limite = maintenant.AddDays(CommandeService.JoursAlerteEcheance);

        return new CommandesResumeDto(
            actives.Count(c => c.Status == CustomOrderStatus.Commande),
            actives.Count,
            actives.Count(c => c.Deadline >= maintenant && c.Deadline <= limite),
            actives.Count(c => c.Deadline < maintenant),
            actives.Count(c => c.Status == CustomOrderStatus.Pret));
    }

    private async Task<StockResumeDto> StockAsync(CancellationToken cancellationToken)
    {
        var produits = await _context.Products
            .AsNoTracking().Where(p => p.IsActive)
            .Select(p => new { p.CurrentStock, p.MinimumStock, p.ProductionCost })
            .ToListAsync(cancellationToken);

        return new StockResumeDto(
            produits.Count,
            produits.Count(p => p.CurrentStock <= p.MinimumStock),
            Math.Round(produits.Sum(p => p.CurrentStock * p.ProductionCost), 2));
    }

    /// <summary>
    /// Les produits passés sous leur seuil, montrés directement sur le
    /// tableau de bord : c'est le seul rappel dont l'atelier a besoin, et il
    /// évite tout un centre de notifications.
    /// </summary>
    private async Task<IReadOnlyList<ProduitAlerteDto>> StockFaibleAsync(
        CancellationToken cancellationToken)
        => await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStock)
            .OrderBy(p => p.CurrentStock)
            .Take(NombreClassement)
            .Select(p => new ProduitAlerteDto(p.Id, p.Name, p.CurrentStock, p.MinimumStock))
            .ToListAsync(cancellationToken);

    private async Task<FinancesResumeDto> FinancesAsync(
        decimal depensesDuMois, CancellationToken cancellationToken)
    {
        var encaisse = await _context.Payments.AsNoTracking()
            .SumAsync(p => (decimal?)p.Amount, cancellationToken) ?? 0m;

        var ventesDues = await _context.Sales.AsNoTracking()
            .Where(v => v.Status == SaleStatus.Confirmee)
            .SumAsync(v => (decimal?)(v.TotalAmount - v.PaidAmount), cancellationToken) ?? 0m;

        var commandesDues = await _context.CustomOrders.AsNoTracking()
            .Where(c => c.Status != CustomOrderStatus.Annule)
            .SumAsync(c => (decimal?)(c.TotalAmount - c.PaidAmount), cancellationToken) ?? 0m;

        return new FinancesResumeDto(
            encaisse,
            Math.Max(0m, ventesDues + commandesDues),
            depensesDuMois);
    }

    /// <summary>Vente réduite aux seules valeurs nécessaires aux graphiques.</summary>
    private sealed record LigneVente(DateTime Date, decimal Total, decimal Benefice);

    private static IReadOnlyList<PointGraphiqueDto> ConstruireParJour(
        IReadOnlyList<LigneVente> ventes, DateTime debut)
    {
        var points = new List<PointGraphiqueDto>();

        for (var jour = 0; jour < JoursGraphique; jour++)
        {
            var date = debut.AddDays(jour);
            var total = ventes.Where(v => v.Date.Date == date.Date).Sum(v => v.Total);

            points.Add(new PointGraphiqueDto(date.ToString("dd/MM"), Math.Round(total, 2)));
        }

        return points;
    }

    private static IReadOnlyList<PointGraphiqueDto> ConstruireParMois(
        IReadOnlyList<LigneVente> ventes, DateTime debut, Func<LigneVente, decimal> valeur)
    {
        var points = new List<PointGraphiqueDto>();

        for (var mois = 0; mois < MoisGraphique; mois++)
        {
            var date = debut.AddMonths(mois);
            var total = ventes
                .Where(v => v.Date.Year == date.Year && v.Date.Month == date.Month)
                .Sum(valeur);

            points.Add(new PointGraphiqueDto(
                date.ToString("MMM yy", ParametresAtelier.Culture), Math.Round(total, 2)));
        }

        return points;
    }

    /// <summary>Ligne de vente réduite aux valeurs nécessaires aux classements.</summary>
    private sealed record LigneClassement(string Nom, decimal Quantite, decimal Montant, decimal Benefice);

    /// <summary>
    /// Les classements sont regroupés en mémoire : l'atelier manipule des volumes
    /// modestes et le comportement reste identique quel que soit le moteur de base.
    /// </summary>
    private async Task<List<LigneClassement>> LignesVenduesAsync(CancellationToken cancellationToken)
        => await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == SaleStatus.Confirmee)
            .Select(i => new LigneClassement(
                i.Product.Name, i.Quantity, i.LineTotal, i.LineTotal - i.Quantity * i.UnitCost))
            .ToListAsync(cancellationToken);

    private static List<ClassementDto> Classer(
        IEnumerable<LigneClassement> lignes, Func<ClassementDto, decimal> tri)
        => lignes
            .GroupBy(l => l.Nom)
            .Select(g => new ClassementDto(
                g.Key, g.Sum(l => l.Quantite), g.Sum(l => l.Montant), "pièce"))
            .OrderByDescending(tri)
            .Take(NombreClassement)
            .ToList();

    private async Task<IReadOnlyList<ClassementDto>> ProduitsLesPlusVendusAsync(
        CancellationToken cancellationToken)
        => Classer(await LignesVenduesAsync(cancellationToken), c => c.Quantite);
}
