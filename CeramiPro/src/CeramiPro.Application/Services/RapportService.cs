using System.Text;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Produit les rapports de l'atelier sur une période donnée : chiffre d'affaires,
/// bénéfices, dépenses, dettes, consommation des matières, production, pièces
/// endommagées, produits les plus vendus et les plus rentables, valeur du stock.
/// </summary>
public class RapportService : IRapportService
{
    private readonly IApplicationDbContext _context;
    private readonly IServiceDateHeure _horloge;

    public RapportService(IApplicationDbContext context, IServiceDateHeure horloge)
    {
        _context = context;
        _horloge = horloge;
    }

    public async Task<RapportDto> GenererAsync(
        RapportRequete requete, CancellationToken cancellationToken = default)
    {
        var (du, au) = Periode(requete);
        var periode = $"Du {Formatage.Date(du)} au {Formatage.Date(au.AddDays(-1))}";

        return requete.Type switch
        {
            TypeRapport.ChiffreAffaires => await ChiffreAffairesAsync(du, au, periode, cancellationToken),
            TypeRapport.Benefices => await BeneficesAsync(du, au, periode, cancellationToken),
            TypeRapport.Depenses => await DepensesAsync(du, au, periode, cancellationToken),
            TypeRapport.DettesClients => await DettesClientsAsync(periode, cancellationToken),
            TypeRapport.ProduitsLesPlusVendus => await PlusVendusAsync(du, au, periode, cancellationToken),
            TypeRapport.ProduitsLesPlusRentables => await PlusRentablesAsync(du, au, periode, cancellationToken),
            TypeRapport.ValeurStock => await ValeurStockAsync(periode, cancellationToken),
            _ => await ValeurStockAsync(periode, cancellationToken)
        };
    }

    public async Task<(string NomFichier, byte[] Contenu)> ExporterCsvAsync(
        RapportRequete requete, CancellationToken cancellationToken = default)
    {
        var rapport = await GenererAsync(requete, cancellationToken);
        var texte = new StringBuilder();

        texte.AppendLine(Echapper(rapport.Titre));
        texte.AppendLine(Echapper(rapport.Periode));
        texte.AppendLine();
        texte.AppendLine(string.Join(';', rapport.Colonnes.Select(Echapper)));

        foreach (var ligne in rapport.Lignes)
        {
            texte.AppendLine(string.Join(';', ligne.Select(Echapper)));
        }

        if (rapport.Totaux is not null)
        {
            texte.AppendLine(string.Join(';', rapport.Totaux.Select(Echapper)));
        }

        var nom = $"{rapport.Titre.Replace(' ', '-').ToLowerInvariant()}-" +
                  $"{_horloge.Aujourdhui:yyyy-MM-dd}.csv";

        // Le BOM permet à Excel d'ouvrir directement le fichier avec les accents.
        var contenu = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(texte.ToString())).ToArray();

        return (nom, contenu);
    }

    // ------------------------------------------------------------- Rapports

    private async Task<RapportDto> ChiffreAffairesAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var ventes = await _context.Sales.AsNoTracking()
            .Where(v => v.Status == SaleStatus.Confirmee && v.SaleDate >= du && v.SaleDate < au)
            .Select(v => new { v.SaleDate, v.TotalAmount, v.TaxAmount, v.TotalCost })
            .ToListAsync(cancellationToken);

        var lignes = ventes
            .GroupBy(v => v.SaleDate.Date)
            .OrderBy(g => g.Key)
            .Select(g => (IReadOnlyList<string>)new List<string>
            {
                Formatage.Date(g.Key),
                g.Count().ToString(),
                Formatage.Montant(g.Sum(v => v.TotalAmount)),
                Formatage.Montant(g.Sum(v => v.TotalAmount - v.TaxAmount - v.TotalCost))
            })
            .ToList();

        return new RapportDto(
            TypeRapport.ChiffreAffaires,
            "Chiffre d'affaires",
            periode,
            new[] { "Date", "Ventes", "Chiffre d'affaires", "Bénéfice" },
            lignes,
            new[]
            {
                "Total", ventes.Count.ToString(),
                Formatage.Montant(ventes.Sum(v => v.TotalAmount)),
                Formatage.Montant(ventes.Sum(v => v.TotalAmount - v.TaxAmount - v.TotalCost))
            },
            ventes.GroupBy(v => v.SaleDate.Date).OrderBy(g => g.Key)
                .Select(g => new PointGraphiqueDto(g.Key.ToString("dd/MM"), g.Sum(v => v.TotalAmount)))
                .ToList());
    }

    private async Task<RapportDto> BeneficesAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var ventes = await _context.Sales.AsNoTracking()
            .Where(v => v.Status == SaleStatus.Confirmee && v.SaleDate >= du && v.SaleDate < au)
            .Select(v => new { v.SaleDate, v.TotalAmount, v.TaxAmount, v.TotalCost })
            .ToListAsync(cancellationToken);

        var depenses = await _context.Expenses.AsNoTracking()
            .Where(d => d.ExpenseDate >= du && d.ExpenseDate < au)
            .Select(d => new { d.ExpenseDate, d.Amount })
            .ToListAsync(cancellationToken);

        var mois = ventes.Select(v => new DateTime(v.SaleDate.Year, v.SaleDate.Month, 1))
            .Concat(depenses.Select(d => new DateTime(d.ExpenseDate.Year, d.ExpenseDate.Month, 1)))
            .Distinct().OrderBy(m => m).ToList();

        var lignes = mois.Select(m =>
        {
            var chiffre = ventes.Where(v => v.SaleDate.Year == m.Year && v.SaleDate.Month == m.Month)
                .Sum(v => v.TotalAmount);
            var marge = ventes.Where(v => v.SaleDate.Year == m.Year && v.SaleDate.Month == m.Month)
                .Sum(v => v.TotalAmount - v.TaxAmount - v.TotalCost);
            var charges = depenses.Where(d => d.ExpenseDate.Year == m.Year && d.ExpenseDate.Month == m.Month)
                .Sum(d => d.Amount);

            return (IReadOnlyList<string>)new List<string>
            {
                m.ToString("MMMM yyyy", ParametresAtelier.Culture),
                Formatage.Montant(chiffre),
                Formatage.Montant(marge),
                Formatage.Montant(charges),
                Formatage.Montant(marge - charges)
            };
        }).ToList();

        var margeTotale = ventes.Sum(v => v.TotalAmount - v.TaxAmount - v.TotalCost);
        var chargesTotales = depenses.Sum(d => d.Amount);

        return new RapportDto(
            TypeRapport.Benefices,
            "Bénéfices",
            periode,
            new[] { "Mois", "Chiffre d'affaires", "Marge sur ventes", "Dépenses", "Résultat" },
            lignes,
            new[]
            {
                "Total",
                Formatage.Montant(ventes.Sum(v => v.TotalAmount)),
                Formatage.Montant(margeTotale),
                Formatage.Montant(chargesTotales),
                Formatage.Montant(margeTotale - chargesTotales)
            },
            null);
    }

    private async Task<RapportDto> DepensesAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var depenses = await _context.Expenses
            .Include(d => d.ExpenseCategory)
            .AsNoTracking()
            .Where(d => d.ExpenseDate >= du && d.ExpenseDate < au)
            .Select(d => new { Categorie = d.ExpenseCategory.Name, d.Amount })
            .ToListAsync(cancellationToken);

        var lignes = depenses
            .GroupBy(d => d.Categorie)
            .OrderByDescending(g => g.Sum(d => d.Amount))
            .Select(g => (IReadOnlyList<string>)new List<string>
            {
                g.Key, g.Count().ToString(), Formatage.Montant(g.Sum(d => d.Amount))
            })
            .ToList();

        return new RapportDto(
            TypeRapport.Depenses,
            "Dépenses",
            periode,
            new[] { "Catégorie", "Nombre", "Montant" },
            lignes,
            new[] { "Total", depenses.Count.ToString(), Formatage.Montant(depenses.Sum(d => d.Amount)) },
            depenses.GroupBy(d => d.Categorie)
                .Select(g => new PointGraphiqueDto(g.Key, g.Sum(d => d.Amount)))
                .OrderByDescending(p => p.Valeur).ToList());
    }

    private async Task<RapportDto> DettesClientsAsync(string periode, CancellationToken cancellationToken)
    {
        var clients = await _context.Customers
            .AsNoTracking()
            .Select(c => new
            {
                c.FullName,
                c.PhoneNumber,
                Du = (c.Sales.Where(v => v.Status == SaleStatus.Confirmee)
                          .Sum(v => (decimal?)v.TotalAmount) ?? 0m)
                     + (c.CustomOrders.Where(o => o.Status != CustomOrderStatus.Annule)
                         .Sum(o => (decimal?)o.TotalAmount) ?? 0m),
                Paye = c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var dettes = clients.Where(c => c.Du - c.Paye > 0).OrderByDescending(c => c.Du - c.Paye).ToList();

        return new RapportDto(
            TypeRapport.DettesClients,
            "Dettes clients",
            "Situation à ce jour",
            new[] { "Client", "Téléphone", "Total dû", "Payé", "Reste" },
            dettes.Select(c => (IReadOnlyList<string>)new List<string>
            {
                c.FullName, c.PhoneNumber ?? "—",
                Formatage.Montant(c.Du),
                Formatage.Montant(c.Paye),
                Formatage.Montant(c.Du - c.Paye)
            }).ToList(),
            new[]
            {
                "Total", string.Empty,
                Formatage.Montant(dettes.Sum(c => c.Du)),
                Formatage.Montant(dettes.Sum(c => c.Paye)),
                Formatage.Montant(dettes.Sum(c => c.Du - c.Paye))
            },
            null);
    }

    private async Task<RapportDto> PlusVendusAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var lignesVente = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == SaleStatus.Confirmee
                        && i.Sale.SaleDate >= du && i.Sale.SaleDate < au)
            .Select(i => new { Produit = i.Product.Name, i.Quantity, i.LineTotal })
            .ToListAsync(cancellationToken);

        var lignes = lignesVente
            .GroupBy(i => i.Produit)
            .OrderByDescending(g => g.Sum(i => i.Quantity))
            .Select(g => (IReadOnlyList<string>)new List<string>
            {
                g.Key,
                Formatage.Quantite(g.Sum(i => i.Quantity)),
                Formatage.Montant(g.Sum(i => i.LineTotal))
            })
            .ToList();

        return new RapportDto(
            TypeRapport.ProduitsLesPlusVendus,
            "Produits les plus vendus",
            periode,
            new[] { "Produit", "Quantité vendue", "Chiffre d'affaires" },
            lignes,
            new[]
            {
                "Total",
                Formatage.Quantite(lignesVente.Sum(i => i.Quantity)),
                Formatage.Montant(lignesVente.Sum(i => i.LineTotal))
            },
            lignesVente.GroupBy(i => i.Produit)
                .Select(g => new PointGraphiqueDto(g.Key, g.Sum(i => i.Quantity)))
                .OrderByDescending(p => p.Valeur).Take(10).ToList());
    }

    private async Task<RapportDto> PlusRentablesAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var lignesVente = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == SaleStatus.Confirmee
                        && i.Sale.SaleDate >= du && i.Sale.SaleDate < au)
            .Select(i => new
            {
                Produit = i.Product.Name, i.Quantity, i.LineTotal,
                Cout = i.Quantity * i.UnitCost
            })
            .ToListAsync(cancellationToken);

        var lignes = lignesVente
            .GroupBy(i => i.Produit)
            .OrderByDescending(g => g.Sum(i => i.LineTotal - i.Cout))
            .Select(g => (IReadOnlyList<string>)new List<string>
            {
                g.Key,
                Formatage.Quantite(g.Sum(i => i.Quantity)),
                Formatage.Montant(g.Sum(i => i.LineTotal)),
                Formatage.Montant(g.Sum(i => i.Cout)),
                Formatage.Montant(g.Sum(i => i.LineTotal - i.Cout))
            })
            .ToList();

        return new RapportDto(
            TypeRapport.ProduitsLesPlusRentables,
            "Produits les plus rentables",
            periode,
            new[] { "Produit", "Quantité", "Chiffre d'affaires", "Coût de revient", "Bénéfice" },
            lignes,
            new[]
            {
                "Total",
                Formatage.Quantite(lignesVente.Sum(i => i.Quantity)),
                Formatage.Montant(lignesVente.Sum(i => i.LineTotal)),
                Formatage.Montant(lignesVente.Sum(i => i.Cout)),
                Formatage.Montant(lignesVente.Sum(i => i.LineTotal - i.Cout))
            },
            null);
    }

    private async Task<RapportDto> ValeurStockAsync(string periode, CancellationToken cancellationToken)
    {
        var produits = await _context.Products
            .Include(p => p.ProductCategory)
            .AsNoTracking().Where(p => p.IsActive)
            .Select(p => new
            {
                Categorie = p.ProductCategory.Name, p.Name, p.CurrentStock,
                p.ProductionCost, p.SellingPrice
            })
            .ToListAsync(cancellationToken);

        var enStock = produits
            .Where(p => p.CurrentStock != 0)
            .OrderByDescending(p => p.CurrentStock * p.ProductionCost)
            .ToList();

        var lignes = enStock
            .Select(p => (IReadOnlyList<string>)new List<string>
            {
                p.Categorie,
                p.Name,
                Formatage.Quantite(p.CurrentStock),
                Formatage.Montant(p.ProductionCost),
                Formatage.Montant(p.CurrentStock * p.ProductionCost),
                Formatage.Montant(p.CurrentStock * p.SellingPrice)
            })
            .ToList();

        return new RapportDto(
            TypeRapport.ValeurStock,
            "Valeur du stock",
            periode,
            new[] { "Catégorie", "Produit", "En stock", "Coût unitaire", "Valeur au coût", "Valeur à la vente" },
            lignes,
            new[]
            {
                "Total",
                string.Empty,
                Formatage.Quantite(enStock.Sum(p => p.CurrentStock)),
                string.Empty,
                Formatage.Montant(enStock.Sum(p => p.CurrentStock * p.ProductionCost)),
                Formatage.Montant(enStock.Sum(p => p.CurrentStock * p.SellingPrice))
            },
            null);
    }

    /// <summary>Période retenue : par défaut le mois en cours.</summary>
    private (DateTime Du, DateTime Au) Periode(RapportRequete requete)
    {
        var aujourdhui = _horloge.Aujourdhui;

        var du = requete.Du?.Date ?? new DateTime(aujourdhui.Year, aujourdhui.Month, 1);
        var au = (requete.Au?.Date ?? aujourdhui).AddDays(1);

        return (DateTime.SpecifyKind(du, DateTimeKind.Utc), DateTime.SpecifyKind(au, DateTimeKind.Utc));
    }

    /// <summary>Protège les valeurs contenant un point-virgule ou un guillemet.</summary>
    private static string Echapper(string valeur)
        => valeur.Contains(';') || valeur.Contains('"') || valeur.Contains('\n')
            ? $"\"{valeur.Replace("\"", "\"\"")}\""
            : valeur;
}
