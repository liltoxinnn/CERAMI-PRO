using System.Text;
using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Rapports de l'atelier sur une période : ventes par jour et par mois,
/// bénéfices, dépenses, dettes des clients, produits les plus vendus et les
/// plus rentables, état du stock.
///
/// Les périodes s'entendent en jours de l'atelier, à l'heure d'Alger.
/// </summary>
public class RapportService : IRapportService
{
    /// <summary>Au-delà, un graphique jour par jour devient illisible : seuls les jours actifs y figurent.</summary>
    private const int JoursGraphiqueComplet = 62;

    /// <summary>Rappel affiché sous les rapports qui comptent le chiffre d'affaires.</summary>
    private const string RemarqueChiffreAffaires =
        "Chiffre d'affaires = ventes au comptoir et commandes personnalisées livrées sur la période. " +
        "Un acompte reçu avant la livraison figure dans « Encaissé », pas encore dans le chiffre d'affaires.";

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
        var periode = Periode(requete);

        return requete.Type switch
        {
            TypeRapport.VentesParJour => await VentesParJourAsync(periode, cancellationToken),
            TypeRapport.VentesParMois => await VentesParMoisAsync(periode, cancellationToken),
            TypeRapport.Benefices => await BeneficesAsync(periode, cancellationToken),
            TypeRapport.Depenses => await DepensesAsync(periode, cancellationToken),
            TypeRapport.DettesClients => await DettesClientsAsync(cancellationToken),
            TypeRapport.ProduitsLesPlusVendus => await PlusVendusAsync(periode, cancellationToken),
            TypeRapport.ProduitsLesPlusRentables => await PlusRentablesAsync(periode, cancellationToken),
            _ => await EtatDuStockAsync(cancellationToken)
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

    private async Task<RapportDto> VentesParJourAsync(PeriodeAtelier periode, CancellationToken cancellationToken)
    {
        var activite = await ActiviteCommerciale.ChargerAsync(_context, _horloge, periode, cancellationToken);

        var jours = Enumerable.Range(0, (periode.DernierJour - periode.PremierJour).Days + 1)
            .Select(decalage => periode.PremierJour.AddDays(decalage))
            .Select(jour => (Jour: jour, Bilan: activite.Sur(j => j == jour)))
            .ToList();

        var actifs = jours.Where(j => !j.Bilan.EstVide).ToList();
        var graphique = jours.Count <= JoursGraphiqueComplet ? jours : actifs;

        return new RapportDto(
            TypeRapport.VentesParJour,
            "Ventes par jour",
            periode.Libelle,
            ColonnesVentes("Jour"),
            actifs.Select(j => LigneVentes(Formatage.Date(j.Jour), j.Bilan)).ToList(),
            LigneVentes("Total", activite.Total),
            graphique.Select(j => new PointGraphiqueDto(j.Jour.ToString("dd/MM"), j.Bilan.ChiffreAffaires)).ToList(),
            RemarqueChiffreAffaires);
    }

    private async Task<RapportDto> VentesParMoisAsync(PeriodeAtelier periode, CancellationToken cancellationToken)
    {
        var activite = await ActiviteCommerciale.ChargerAsync(_context, _horloge, periode, cancellationToken);
        var mois = MoisDe(periode).Select(m => (Mois: m, Bilan: activite.Sur(j => MemeMois(j, m)))).ToList();

        return new RapportDto(
            TypeRapport.VentesParMois,
            "Ventes par mois",
            periode.Libelle,
            ColonnesVentes("Mois"),
            mois.Select(m => LigneVentes(NomDuMois(m.Mois), m.Bilan)).ToList(),
            LigneVentes("Total", activite.Total),
            mois.Select(m => new PointGraphiqueDto(
                m.Mois.ToString("MMM yy", ParametresAtelier.Culture), m.Bilan.ChiffreAffaires)).ToList(),
            RemarqueChiffreAffaires);
    }

    private static IReadOnlyList<string> ColonnesVentes(string periode) => new[]
    {
        periode, "Ventes", "Montant des ventes", "Commandes livrées", "Chiffre d'affaires", "Encaissé"
    };

    private static IReadOnlyList<string> LigneVentes(string libelle, Bilan bilan) => new[]
    {
        libelle,
        bilan.NombreVentes.ToString(ParametresAtelier.Culture),
        Formatage.Montant(bilan.MontantVentes),
        bilan.NombreLivraisons == 0
            ? "—"
            : $"{bilan.NombreLivraisons} · {Formatage.Montant(bilan.MontantLivraisons)}",
        Formatage.Montant(bilan.ChiffreAffaires),
        Formatage.Montant(bilan.Encaisse)
    };

    private async Task<RapportDto> BeneficesAsync(PeriodeAtelier periode, CancellationToken cancellationToken)
    {
        var activite = await ActiviteCommerciale.ChargerAsync(_context, _horloge, periode, cancellationToken);
        var mois = MoisDe(periode).Select(m => (Mois: m, Bilan: activite.Sur(j => MemeMois(j, m)))).ToList();

        static IReadOnlyList<string> Ligne(string libelle, Bilan bilan) => new[]
        {
            libelle,
            Formatage.Montant(bilan.ChiffreAffaires),
            Formatage.Montant(bilan.Tva),
            Formatage.Montant(bilan.CoutVentes),
            Formatage.Montant(bilan.Depenses),
            Formatage.Montant(bilan.Resultat)
        };

        return new RapportDto(
            TypeRapport.Benefices,
            "Bénéfices",
            periode.Libelle,
            new[] { "Mois", "Chiffre d'affaires", "TVA", "Coût des pièces vendues", "Dépenses", "Bénéfice" },
            mois.Select(m => Ligne(NomDuMois(m.Mois), m.Bilan)).ToList(),
            Ligne("Total", activite.Total),
            mois.Select(m => new PointGraphiqueDto(
                m.Mois.ToString("MMM yy", ParametresAtelier.Culture), m.Bilan.Resultat)).ToList(),
            "Bénéfice = chiffre d'affaires − TVA − coût des pièces vendues − dépenses. Ce que coûte une " +
            "commande personnalisée (matières, emballage, transport) se saisit dans les dépenses.");
    }

    private async Task<RapportDto> DepensesAsync(
        PeriodeAtelier periode, CancellationToken cancellationToken)
    {
        var depenses = await _context.Expenses
            .Include(d => d.ExpenseCategory)
            .AsNoTracking()
            .Where(d => d.ExpenseDate >= periode.DebutUtc && d.ExpenseDate < periode.FinUtc)
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
            periode.Libelle,
            new[] { "Catégorie", "Nombre", "Montant" },
            lignes,
            new[] { "Total", depenses.Count.ToString(), Formatage.Montant(depenses.Sum(d => d.Amount)) },
            depenses.GroupBy(d => d.Categorie)
                .Select(g => new PointGraphiqueDto(g.Key, g.Sum(d => d.Amount)))
                .OrderByDescending(p => p.Valeur).ToList());
    }

    private async Task<RapportDto> DettesClientsAsync(CancellationToken cancellationToken)
    {
        var clients = await _context.Customers
            .AsNoTracking()
            .Select(c => new
            {
                c.FullName,
                c.PhoneNumber,
                Du = (c.Sales.Where(v => v.Status == SaleStatus.Confirmee)
                          .Sum(v => (decimal?)v.TotalAmount) ?? 0m)
                     + (c.CustomOrders.Where(o => o.Status != CustomOrderStatus.Annulee)
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
        PeriodeAtelier periode, CancellationToken cancellationToken)
    {
        var lignesVente = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == SaleStatus.Confirmee
                        && i.Sale.SaleDate >= periode.DebutUtc && i.Sale.SaleDate < periode.FinUtc)
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
            periode.Libelle,
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
        PeriodeAtelier periode, CancellationToken cancellationToken)
    {
        var lignesVente = await _context.SaleItems
            .AsNoTracking()
            .Where(i => i.Sale.Status == SaleStatus.Confirmee
                        && i.Sale.SaleDate >= periode.DebutUtc && i.Sale.SaleDate < periode.FinUtc)
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
            periode.Libelle,
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
            null,
            "Bénéfice = prix de vente moins le coût de revient de la pièce au moment de la vente.");
    }

    /// <summary>
    /// Tous les produits actifs, avec leur état : c'est le résumé du stock
    /// que l'on relit avant de relancer une fabrication.
    /// </summary>
    private async Task<RapportDto> EtatDuStockAsync(CancellationToken cancellationToken)
    {
        var produits = await _context.Products
            .AsNoTracking().Where(p => p.IsActive)
            .OrderBy(p => p.ProductCategory.Name).ThenBy(p => p.Name)
            .Select(p => new
            {
                Categorie = p.ProductCategory.Name, p.Name, p.CurrentStock, p.MinimumStock,
                p.IsUniquePiece, p.ProductionCost, p.SellingPrice
            })
            .ToListAsync(cancellationToken);

        string Etat(decimal stock, decimal seuil, bool unique) => (stock, unique) switch
        {
            (<= 0, true) => "Vendue",
            (> 0, true) => "Pièce unique",
            (<= 0, false) => "Rupture",
            _ when stock <= seuil => "Stock faible",
            _ => "Disponible"
        };

        var lignes = produits.Select(p => (IReadOnlyList<string>)new List<string>
        {
            p.Categorie,
            p.Name,
            Formatage.Quantite(p.CurrentStock),
            p.IsUniquePiece ? "—" : Formatage.Quantite(p.MinimumStock),
            Etat(p.CurrentStock, p.MinimumStock, p.IsUniquePiece),
            Formatage.Montant(Math.Max(0m, p.CurrentStock) * p.ProductionCost),
            Formatage.Montant(Math.Max(0m, p.CurrentStock) * p.SellingPrice)
        }).ToList();

        var aSurveiller = produits.Count(p => !p.IsUniquePiece && p.CurrentStock <= p.MinimumStock);

        return new RapportDto(
            TypeRapport.ValeurStock,
            "État du stock",
            $"Situation au {Formatage.Date(_horloge.Aujourdhui)}",
            new[] { "Catégorie", "Produit", "En stock", "Seuil", "État", "Valeur au coût", "Valeur à la vente" },
            lignes,
            new[]
            {
                "Total", $"{produits.Count} produit(s)",
                Formatage.Quantite(produits.Sum(p => Math.Max(0m, p.CurrentStock))),
                string.Empty, $"{aSurveiller} à surveiller",
                Formatage.Montant(produits.Sum(p => Math.Max(0m, p.CurrentStock) * p.ProductionCost)),
                Formatage.Montant(produits.Sum(p => Math.Max(0m, p.CurrentStock) * p.SellingPrice))
            },
            null,
            "« Stock faible » : quantité égale ou inférieure au seuil d'alerte. " +
            "Une pièce unique vendue n'est pas une rupture : elle n'est pas refabriquée.");
    }

    // ------------------------------------------------------------- Outils

    /// <summary>
    /// Période retenue. Par défaut : le mois en cours, ou l'année en cours
    /// pour les rapports qui se lisent mois par mois.
    /// </summary>
    private PeriodeAtelier Periode(RapportRequete requete)
    {
        var aujourdhui = _horloge.Aujourdhui;
        var parMois = requete.Type is TypeRapport.VentesParMois or TypeRapport.Benefices;

        var premier = requete.Du?.Date
                      ?? (parMois ? new DateTime(aujourdhui.Year, 1, 1) : new DateTime(aujourdhui.Year, aujourdhui.Month, 1));
        var dernier = requete.Au?.Date ?? aujourdhui;

        return PeriodeAtelier.Jours(_horloge, premier, dernier);
    }

    /// <summary>Premier jour de chacun des mois couverts par la période.</summary>
    private static IEnumerable<DateTime> MoisDe(PeriodeAtelier periode)
    {
        for (var mois = new DateTime(periode.PremierJour.Year, periode.PremierJour.Month, 1);
             mois <= periode.DernierJour;
             mois = mois.AddMonths(1))
        {
            yield return mois;
        }
    }

    private static bool MemeMois(DateTime jour, DateTime mois) => jour.Year == mois.Year && jour.Month == mois.Month;

    /// <summary>« Novembre 2026 ».</summary>
    private static string NomDuMois(DateTime mois)
    {
        var nom = mois.ToString("MMMM yyyy", ParametresAtelier.Culture);
        return char.ToUpper(nom[0], ParametresAtelier.Culture) + nom[1..];
    }

    /// <summary>Protège les valeurs contenant un point-virgule ou un guillemet.</summary>
    private static string Echapper(string valeur)
        => valeur.Contains(';') || valeur.Contains('"') || valeur.Contains('\n')
            ? $"\"{valeur.Replace("\"", "\"\"")}\""
            : valeur;
}
