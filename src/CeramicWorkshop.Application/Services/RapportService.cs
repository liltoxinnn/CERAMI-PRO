using System.Text;
using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Finances;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Produit les rapports de l'atelier sur une période donnée : chiffre d'affaires,
/// bénéfices, dépenses, dettes, consommation des matières, production, pièces
/// endommagées, produits les plus vendus et les plus rentables, valeur du stock.
/// </summary>
public class RapportService : IRapportService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _horloge;

    public RapportService(IApplicationDbContext context, IDateTimeService horloge)
    {
        _context = context;
        _horloge = horloge;
    }

    public async Task<RapportDto> GenererAsync(
        RapportRequete requete, CancellationToken cancellationToken = default)
    {
        var (du, au) = Periode(requete);
        var periode = $"Du {MontantFormatter.FormaterDate(du)} au {MontantFormatter.FormaterDate(au.AddDays(-1))}";

        return requete.Type switch
        {
            TypeRapport.ChiffreAffaires => await ChiffreAffairesAsync(du, au, periode, cancellationToken),
            TypeRapport.Benefices => await BeneficesAsync(du, au, periode, cancellationToken),
            TypeRapport.Depenses => await DepensesAsync(du, au, periode, cancellationToken),
            TypeRapport.DettesClients => await DettesClientsAsync(periode, cancellationToken),
            TypeRapport.DettesFournisseurs => await DettesFournisseursAsync(periode, cancellationToken),
            TypeRapport.ConsommationMatieres => await ConsommationAsync(du, au, periode, cancellationToken),
            TypeRapport.Production => await ProductionAsync(du, au, periode, cancellationToken),
            TypeRapport.ProduitsEndommages => await EndommagesAsync(du, au, periode, cancellationToken),
            TypeRapport.ProduitsLesPlusVendus => await PlusVendusAsync(du, au, periode, cancellationToken),
            TypeRapport.ProduitsLesPlusRentables => await PlusRentablesAsync(du, au, periode, cancellationToken),
            TypeRapport.ValeurStock => await ValeurStockAsync(periode, cancellationToken),
            _ => await PerformanceProductionAsync(du, au, periode, cancellationToken)
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
                  $"{_horloge.AujourdHui:yyyy-MM-dd}.csv";

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
                MontantFormatter.FormaterDate(g.Key),
                g.Count().ToString(),
                MontantFormatter.Formater(g.Sum(v => v.TotalAmount)),
                MontantFormatter.Formater(g.Sum(v => v.TotalAmount - v.TaxAmount - v.TotalCost))
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
                MontantFormatter.Formater(ventes.Sum(v => v.TotalAmount)),
                MontantFormatter.Formater(ventes.Sum(v => v.TotalAmount - v.TaxAmount - v.TotalCost))
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
                m.ToString("MMMM yyyy", MontantFormatter.CultureAtelier),
                MontantFormatter.Formater(chiffre),
                MontantFormatter.Formater(marge),
                MontantFormatter.Formater(charges),
                MontantFormatter.Formater(marge - charges)
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
                MontantFormatter.Formater(ventes.Sum(v => v.TotalAmount)),
                MontantFormatter.Formater(margeTotale),
                MontantFormatter.Formater(chargesTotales),
                MontantFormatter.Formater(margeTotale - chargesTotales)
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
                g.Key, g.Count().ToString(), MontantFormatter.Formater(g.Sum(d => d.Amount))
            })
            .ToList();

        return new RapportDto(
            TypeRapport.Depenses,
            "Dépenses",
            periode,
            new[] { "Catégorie", "Nombre", "Montant" },
            lignes,
            new[] { "Total", depenses.Count.ToString(), MontantFormatter.Formater(depenses.Sum(d => d.Amount)) },
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
                MontantFormatter.Formater(c.Du),
                MontantFormatter.Formater(c.Paye),
                MontantFormatter.Formater(c.Du - c.Paye)
            }).ToList(),
            new[]
            {
                "Total", string.Empty,
                MontantFormatter.Formater(dettes.Sum(c => c.Du)),
                MontantFormatter.Formater(dettes.Sum(c => c.Paye)),
                MontantFormatter.Formater(dettes.Sum(c => c.Du - c.Paye))
            },
            null);
    }

    private async Task<RapportDto> DettesFournisseursAsync(string periode, CancellationToken cancellationToken)
    {
        var fournisseurs = await _context.Suppliers
            .AsNoTracking()
            .Select(f => new
            {
                f.Name,
                f.PhoneNumber,
                Du = f.Purchases.Where(a => a.Status != PurchaseStatus.Brouillon
                                            && a.Status != PurchaseStatus.Annule)
                    .Sum(a => (decimal?)a.TotalAmount) ?? 0m,
                Paye = f.SupplierPayments.Sum(r => (decimal?)r.Amount) ?? 0m
            })
            .ToListAsync(cancellationToken);

        var dettes = fournisseurs.Where(f => f.Du - f.Paye > 0).OrderByDescending(f => f.Du - f.Paye).ToList();

        return new RapportDto(
            TypeRapport.DettesFournisseurs,
            "Dettes fournisseurs",
            "Situation à ce jour",
            new[] { "Fournisseur", "Téléphone", "Total acheté", "Payé", "Reste" },
            dettes.Select(f => (IReadOnlyList<string>)new List<string>
            {
                f.Name, f.PhoneNumber ?? "—",
                MontantFormatter.Formater(f.Du),
                MontantFormatter.Formater(f.Paye),
                MontantFormatter.Formater(f.Du - f.Paye)
            }).ToList(),
            new[]
            {
                "Total", string.Empty,
                MontantFormatter.Formater(dettes.Sum(f => f.Du)),
                MontantFormatter.Formater(dettes.Sum(f => f.Paye)),
                MontantFormatter.Formater(dettes.Sum(f => f.Du - f.Paye))
            },
            null);
    }

    private async Task<RapportDto> ConsommationAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var mouvements = await _context.InventoryTransactions
            .AsNoTracking()
            .Where(t => t.TransactionType == InventoryTransactionType.ConsommationProduction
                        && t.Material != null && t.OccurredAt >= du && t.OccurredAt < au)
            .Select(t => new { Nom = t.Material!.Name, Unite = t.Material.Unit.Code, t.Quantity, t.TotalCost })
            .ToListAsync(cancellationToken);

        var lignes = mouvements
            .GroupBy(t => new { t.Nom, t.Unite })
            .OrderByDescending(g => g.Sum(t => t.TotalCost))
            .Select(g => (IReadOnlyList<string>)new List<string>
            {
                g.Key.Nom,
                MontantFormatter.FormaterQuantite(-g.Sum(t => t.Quantity), g.Key.Unite),
                MontantFormatter.Formater(g.Sum(t => t.TotalCost))
            })
            .ToList();

        return new RapportDto(
            TypeRapport.ConsommationMatieres,
            "Consommation des matières",
            periode,
            new[] { "Matière", "Quantité consommée", "Valeur" },
            lignes,
            new[] { "Total", string.Empty, MontantFormatter.Formater(mouvements.Sum(t => t.TotalCost)) },
            null);
    }

    private async Task<RapportDto> ProductionAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var ordres = await _context.ProductionOrders
            .Include(o => o.Product)
            .AsNoTracking()
            .Where(o => o.PlannedStartDate >= du && o.PlannedStartDate < au)
            .Select(o => new
            {
                o.ProductionNumber, Produit = o.Product.Name, o.Status,
                o.PlannedQuantity, o.CompletedQuantity, o.DamagedQuantity,
                o.ActualMaterialCost, o.LaborCost, o.FiringCost, o.DecorationCost,
                o.PackagingCost, o.OtherCost
            })
            .ToListAsync(cancellationToken);

        var lignes = ordres.Select(o => (IReadOnlyList<string>)new List<string>
        {
            o.ProductionNumber,
            o.Produit,
            o.Status.Libelle(),
            MontantFormatter.FormaterQuantite(o.PlannedQuantity),
            MontantFormatter.FormaterQuantite(o.CompletedQuantity),
            MontantFormatter.FormaterQuantite(o.DamagedQuantity),
            MontantFormatter.Formater(o.ActualMaterialCost + o.LaborCost + o.FiringCost
                                      + o.DecorationCost + o.PackagingCost + o.OtherCost)
        }).ToList();

        return new RapportDto(
            TypeRapport.Production,
            "Production",
            periode,
            new[] { "Numéro", "Produit", "État", "Prévu", "Terminé", "Endommagé", "Coût total" },
            lignes,
            new[]
            {
                "Total", string.Empty, string.Empty,
                MontantFormatter.FormaterQuantite(ordres.Sum(o => o.PlannedQuantity)),
                MontantFormatter.FormaterQuantite(ordres.Sum(o => o.CompletedQuantity)),
                MontantFormatter.FormaterQuantite(ordres.Sum(o => o.DamagedQuantity)),
                MontantFormatter.Formater(ordres.Sum(o => o.ActualMaterialCost + o.LaborCost + o.FiringCost
                                                          + o.DecorationCost + o.PackagingCost + o.OtherCost))
            },
            null);
    }

    private async Task<RapportDto> EndommagesAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var etapes = await _context.ProductionStageHistory
            .Include(h => h.ProductionOrder).ThenInclude(o => o.Product)
            .AsNoTracking()
            .Where(h => h.DamagedQuantity > 0 && h.StartedAt >= du && h.StartedAt < au)
            .Select(h => new
            {
                h.ProductionOrder.ProductionNumber,
                Produit = h.ProductionOrder.Product.Name,
                h.Stage, h.DamagedQuantity, h.StartedAt, h.Notes
            })
            .ToListAsync(cancellationToken);

        var lignes = etapes
            .OrderByDescending(e => e.DamagedQuantity)
            .Select(e => (IReadOnlyList<string>)new List<string>
            {
                MontantFormatter.FormaterDate(e.StartedAt),
                e.ProductionNumber,
                e.Produit,
                e.Stage.Libelle(),
                MontantFormatter.FormaterQuantite(e.DamagedQuantity),
                e.Notes ?? "—"
            })
            .ToList();

        return new RapportDto(
            TypeRapport.ProduitsEndommages,
            "Produits endommagés",
            periode,
            new[] { "Date", "Production", "Produit", "Étape", "Pièces", "Observation" },
            lignes,
            new[]
            {
                "Total", string.Empty, string.Empty, string.Empty,
                MontantFormatter.FormaterQuantite(etapes.Sum(e => e.DamagedQuantity)), string.Empty
            },
            etapes.GroupBy(e => e.Stage)
                .Select(g => new PointGraphiqueDto(g.Key.Libelle(), g.Sum(e => e.DamagedQuantity)))
                .OrderByDescending(p => p.Valeur).ToList());
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
                MontantFormatter.FormaterQuantite(g.Sum(i => i.Quantity)),
                MontantFormatter.Formater(g.Sum(i => i.LineTotal))
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
                MontantFormatter.FormaterQuantite(lignesVente.Sum(i => i.Quantity)),
                MontantFormatter.Formater(lignesVente.Sum(i => i.LineTotal))
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
                MontantFormatter.FormaterQuantite(g.Sum(i => i.Quantity)),
                MontantFormatter.Formater(g.Sum(i => i.LineTotal)),
                MontantFormatter.Formater(g.Sum(i => i.Cout)),
                MontantFormatter.Formater(g.Sum(i => i.LineTotal - i.Cout))
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
                MontantFormatter.FormaterQuantite(lignesVente.Sum(i => i.Quantity)),
                MontantFormatter.Formater(lignesVente.Sum(i => i.LineTotal)),
                MontantFormatter.Formater(lignesVente.Sum(i => i.Cout)),
                MontantFormatter.Formater(lignesVente.Sum(i => i.LineTotal - i.Cout))
            },
            null);
    }

    private async Task<RapportDto> ValeurStockAsync(string periode, CancellationToken cancellationToken)
    {
        var matieres = await _context.Materials
            .Include(m => m.Unit).Include(m => m.MaterialCategory)
            .AsNoTracking().Where(m => m.IsActive)
            .Select(m => new
            {
                Categorie = m.MaterialCategory.Name, m.Name, Unite = m.Unit.Code,
                m.CurrentQuantity, m.AverageCost
            })
            .ToListAsync(cancellationToken);

        var produits = await _context.Products
            .Include(p => p.ProductCategory)
            .AsNoTracking().Where(p => p.IsActive)
            .Select(p => new
            {
                Categorie = p.ProductCategory.Name, p.Name, p.CurrentStock, p.ProductionCost
            })
            .ToListAsync(cancellationToken);

        var lignes = new List<IReadOnlyList<string>>();

        foreach (var matiere in matieres.Where(m => m.CurrentQuantity != 0)
                     .OrderByDescending(m => m.CurrentQuantity * m.AverageCost))
        {
            lignes.Add(new List<string>
            {
                "Matière première", matiere.Categorie, matiere.Name,
                MontantFormatter.FormaterQuantite(matiere.CurrentQuantity, matiere.Unite),
                MontantFormatter.Formater(matiere.AverageCost),
                MontantFormatter.Formater(matiere.CurrentQuantity * matiere.AverageCost)
            });
        }

        foreach (var produit in produits.Where(p => p.CurrentStock != 0)
                     .OrderByDescending(p => p.CurrentStock * p.ProductionCost))
        {
            lignes.Add(new List<string>
            {
                "Produit fini", produit.Categorie, produit.Name,
                MontantFormatter.FormaterQuantite(produit.CurrentStock, "pièce"),
                MontantFormatter.Formater(produit.ProductionCost),
                MontantFormatter.Formater(produit.CurrentStock * produit.ProductionCost)
            });
        }

        var valeur = matieres.Sum(m => m.CurrentQuantity * m.AverageCost)
                     + produits.Sum(p => p.CurrentStock * p.ProductionCost);

        return new RapportDto(
            TypeRapport.ValeurStock,
            "Valeur du stock",
            "Situation à ce jour",
            new[] { "Type", "Catégorie", "Article", "Quantité", "Coût unitaire", "Valeur" },
            lignes,
            new[] { "Total", string.Empty, string.Empty, string.Empty, string.Empty,
                MontantFormatter.Formater(valeur) },
            new[]
            {
                new PointGraphiqueDto("Matières premières",
                    Math.Round(matieres.Sum(m => m.CurrentQuantity * m.AverageCost), 2)),
                new PointGraphiqueDto("Produits finis",
                    Math.Round(produits.Sum(p => p.CurrentStock * p.ProductionCost), 2))
            });
    }

    private async Task<RapportDto> PerformanceProductionAsync(
        DateTime du, DateTime au, string periode, CancellationToken cancellationToken)
    {
        var ordres = await _context.ProductionOrders
            .Include(o => o.Product)
            .AsNoTracking()
            .Where(o => o.Status == ProductionStatus.Termine
                        && o.ActualEndDate >= du && o.ActualEndDate < au)
            .Select(o => new
            {
                Produit = o.Product.Name, o.PlannedQuantity, o.CompletedQuantity, o.DamagedQuantity,
                o.ActualStartDate, o.ActualEndDate,
                Cout = o.ActualMaterialCost + o.LaborCost + o.FiringCost
                       + o.DecorationCost + o.PackagingCost + o.OtherCost
            })
            .ToListAsync(cancellationToken);

        var lignes = ordres
            .GroupBy(o => o.Produit)
            .OrderByDescending(g => g.Sum(o => o.CompletedQuantity))
            .Select(g =>
            {
                var prevu = g.Sum(o => o.PlannedQuantity);
                var termine = g.Sum(o => o.CompletedQuantity);
                var casse = g.Sum(o => o.DamagedQuantity);
                var duree = g.Where(o => o.ActualStartDate is not null && o.ActualEndDate is not null)
                    .Select(o => (o.ActualEndDate!.Value - o.ActualStartDate!.Value).TotalDays)
                    .DefaultIfEmpty(0).Average();

                return (IReadOnlyList<string>)new List<string>
                {
                    g.Key,
                    g.Count().ToString(),
                    MontantFormatter.FormaterQuantite(prevu),
                    MontantFormatter.FormaterQuantite(termine),
                    MontantFormatter.FormaterQuantite(casse),
                    prevu > 0 ? $"{Math.Round(termine / prevu * 100m, 1)} %" : "—",
                    $"{Math.Round((decimal)duree, 1)} j",
                    MontantFormatter.Formater(termine > 0 ? g.Sum(o => o.Cout) / termine : 0m)
                };
            })
            .ToList();

        var totalPrevu = ordres.Sum(o => o.PlannedQuantity);
        var totalTermine = ordres.Sum(o => o.CompletedQuantity);

        return new RapportDto(
            TypeRapport.PerformanceProduction,
            "Performance de production",
            periode,
            new[]
            {
                "Produit", "Séries", "Prévu", "Terminé", "Endommagé",
                "Taux de réussite", "Durée moyenne", "Coût par pièce"
            },
            lignes,
            new[]
            {
                "Total", ordres.Count.ToString(),
                MontantFormatter.FormaterQuantite(totalPrevu),
                MontantFormatter.FormaterQuantite(totalTermine),
                MontantFormatter.FormaterQuantite(ordres.Sum(o => o.DamagedQuantity)),
                totalPrevu > 0 ? $"{Math.Round(totalTermine / totalPrevu * 100m, 1)} %" : "—",
                string.Empty, string.Empty
            },
            null);
    }

    /// <summary>Période retenue : par défaut le mois en cours.</summary>
    private (DateTime Du, DateTime Au) Periode(RapportRequete requete)
    {
        var aujourdhui = _horloge.AujourdHui;

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
