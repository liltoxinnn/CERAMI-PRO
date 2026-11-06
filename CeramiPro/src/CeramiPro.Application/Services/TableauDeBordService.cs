using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Rassemble les chiffres du jour et du mois, l'état des commandes, du stock
/// et de ce qui reste à encaisser, ainsi que les graphiques du tableau de bord.
/// Les journées sont celles de l'atelier, à l'heure d'Alger.
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
        var aujourdhui = _horloge.Aujourdhui;
        var debutMois = new DateTime(aujourdhui.Year, aujourdhui.Month, 1);
        var debutGraphiqueJours = aujourdhui.AddDays(-(JoursGraphique - 1));
        var debutGraphiqueMois = debutMois.AddMonths(-(MoisGraphique - 1));

        // Une seule lecture couvre les douze mois du graphique : le jour et
        // le mois en cours en sont des extraits.
        var activite = await ActiviteCommerciale.ChargerAsync(
            _context, _horloge, PeriodeAtelier.Jours(_horloge, debutGraphiqueMois, aujourdhui), cancellationToken);

        var jour = activite.Sur(j => j == aujourdhui);
        var mois = activite.Sur(j => j >= debutMois);

        return new TableauDeBordDto(
            new ActiviteDuJourDto(jour.ChiffreAffaires, jour.NombreVentes, jour.NombreLivraisons, jour.Encaisse),
            new ActivitePeriodeDto(
                mois.ChiffreAffaires, mois.Benefice, mois.Depenses, mois.Resultat,
                mois.NombreVentes, mois.NombreLivraisons, mois.Encaisse),
            await CommandesAsync(cancellationToken),
            await StockAsync(cancellationToken),
            await FinancesAsync(cancellationToken),
            Enumerable.Range(0, JoursGraphique)
                .Select(decalage => debutGraphiqueJours.AddDays(decalage))
                .Select(j => new PointGraphiqueDto(j.ToString("dd/MM"),
                    Math.Round(activite.Sur(x => x == j).ChiffreAffaires, 2)))
                .ToList(),
            Enumerable.Range(0, MoisGraphique)
                .Select(decalage => debutGraphiqueMois.AddMonths(decalage))
                .Select(m => new PointGraphiqueDto(m.ToString("MMM yy", ParametresAtelier.Culture),
                    Math.Round(activite.Sur(x => x.Year == m.Year && x.Month == m.Month).ChiffreAffaires, 2)))
                .ToList(),
            await ProduitsLesPlusVendusAsync(
                PeriodeAtelier.Jours(_horloge, debutGraphiqueJours, aujourdhui), cancellationToken),
            await StockFaibleAsync(cancellationToken));
    }

    /// <summary>
    /// Les échéances se comparent en jours de l'atelier : une commande due
    /// aujourd'hui n'est en retard qu'à partir de demain.
    /// </summary>
    private async Task<CommandesResumeDto> CommandesAsync(CancellationToken cancellationToken)
    {
        var commandes = await _context.CustomOrders
            .AsNoTracking()
            .Where(c => c.Status != CustomOrderStatus.Livree && c.Status != CustomOrderStatus.Annulee)
            .Select(c => new { c.Status, c.Deadline })
            .ToListAsync(cancellationToken);

        var aujourdhui = _horloge.Aujourdhui;
        var limite = aujourdhui.AddDays(CommandeService.JoursAlerteEcheance);
        var actives = commandes
            .Select(c => (c.Status, Echeance: _horloge.EnHeureAtelier(c.Deadline).Date))
            .ToList();

        return new CommandesResumeDto(
            actives.Count(c => c.Status == CustomOrderStatus.Recue),
            actives.Count,
            actives.Count(c => c.Echeance >= aujourdhui && c.Echeance <= limite),
            actives.Count(c => c.Echeance < aujourdhui),
            actives.Count(c => c.Status == CustomOrderStatus.Prete));
    }

    private async Task<StockResumeDto> StockAsync(CancellationToken cancellationToken)
    {
        var produits = await _context.Products
            .AsNoTracking().Where(p => p.IsActive)
            .Select(p => new { p.CurrentStock, p.MinimumStock, p.ProductionCost, p.IsUniquePiece })
            .ToListAsync(cancellationToken);

        return new StockResumeDto(
            produits.Count,
            produits.Count(p => !p.IsUniquePiece && p.CurrentStock <= p.MinimumStock),
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
            .Where(p => p.IsActive && !p.IsUniquePiece && p.CurrentStock <= p.MinimumStock)
            .OrderBy(p => p.CurrentStock)
            .Take(NombreClassement)
            .Select(p => new ProduitAlerteDto(p.Id, p.Name, p.CurrentStock, p.MinimumStock))
            .ToListAsync(cancellationToken);

    /// <summary>
    /// Ce que les clients doivent encore, et ce qu'ils ont déjà versé sur des
    /// commandes en cours : deux chiffres qui se lisent ensemble.
    /// </summary>
    private async Task<FinancesResumeDto> FinancesAsync(CancellationToken cancellationToken)
    {
        var ventesDues = await _context.Sales.AsNoTracking()
            .Where(v => v.Status == SaleStatus.Confirmee && v.TotalAmount > v.PaidAmount)
            .SumAsync(v => (decimal?)(v.TotalAmount - v.PaidAmount), cancellationToken) ?? 0m;

        var commandesDues = await _context.CustomOrders.AsNoTracking()
            .Where(c => c.Status != CustomOrderStatus.Annulee && c.TotalAmount > c.PaidAmount)
            .SumAsync(c => (decimal?)(c.TotalAmount - c.PaidAmount), cancellationToken) ?? 0m;

        var acomptes = await _context.CustomOrders.AsNoTracking()
            .Where(c => c.Status != CustomOrderStatus.Annulee && c.Status != CustomOrderStatus.Livree)
            .SumAsync(c => (decimal?)c.PaidAmount, cancellationToken) ?? 0m;

        return new FinancesResumeDto(ventesDues + commandesDues, acomptes);
    }

    /// <summary>Ligne de vente réduite aux valeurs nécessaires aux classements.</summary>
    private sealed record LigneClassement(string Nom, decimal Quantite, decimal Montant, decimal Benefice);

    /// <summary>
    /// Les classements sont regroupés en mémoire : l'atelier manipule des volumes
    /// modestes et le comportement reste identique quel que soit le moteur de base.
    /// </summary>
    private async Task<List<LigneClassement>> LignesVenduesAsync(
        PeriodeAtelier periode, CancellationToken cancellationToken)
        => await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == SaleStatus.Confirmee
                        && i.Sale.SaleDate >= periode.DebutUtc && i.Sale.SaleDate < periode.FinUtc)
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

    /// <summary>Les pièces qui sont parties le plus sur les trente derniers jours.</summary>
    private async Task<IReadOnlyList<ClassementDto>> ProduitsLesPlusVendusAsync(
        PeriodeAtelier periode, CancellationToken cancellationToken)
        => Classer(await LignesVenduesAsync(periode, cancellationToken), c => c.Quantite);
}
