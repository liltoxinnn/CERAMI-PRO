using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Recherche;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Common;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Recherche globale de l'atelier. Un seul champ suffit pour retrouver un
/// produit, une matière, un client, une commande ou une facture, même si le
/// nom est mal orthographié ou saisi sans accent.
///
/// Chaque famille n'est parcourue que si l'utilisateur a le droit de la
/// consulter : la recherche ne révèle jamais un module fermé.
/// </summary>
public class RechercheService : IRechercheService
{
    /// <summary>Longueur minimale du terme cherché.</summary>
    public const int LongueurMinimale = 2;

    /// <summary>Nombre de fiches examinées par famille avant classement.</summary>
    private const int PlafondExamen = 400;

    private readonly IApplicationDbContext _context;
    private readonly IUtilisateurCourant _utilisateurCourant;

    public RechercheService(IApplicationDbContext context, IUtilisateurCourant utilisateurCourant)
    {
        _context = context;
        _utilisateurCourant = utilisateurCourant;
    }

    public async Task<RechercheGlobaleDto> ChercherAsync(
        string terme, int maximumParFamille = 5, CancellationToken cancellationToken = default)
    {
        var recherche = (terme ?? string.Empty).Trim();

        if (recherche.Length < LongueurMinimale)
        {
            return new RechercheGlobaleDto(recherche, 0, Array.Empty<GroupeResultatsDto>());
        }

        var maximum = Math.Clamp(maximumParFamille, 1, 25);
        var motif = recherche.ToLowerInvariant();
        var groupes = new List<GroupeResultatsDto>();

        await AjouterAsync(groupes, FamilleResultat.Produit, "Produits",
            PermissionCodes.ProduitsConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Products.AsNoTracking(), balayage,
                        p => p.Name.ToLower().Contains(motif)
                                || p.Reference.ToLower().Contains(motif)
                                || (p.Barcode != null && p.Barcode.ToLower().Contains(motif)))
                    .Select(p => new { p.Id, p.Name, p.Reference, p.SellingPrice, p.CurrentStock })
                    .ToListAsync(cancellationToken))
                .Select(p => new Candidat(p.Id, p.Name, p.Reference,
                    $"{Formatage.Montant(p.SellingPrice)} — " +
                    $"{Formatage.Quantite(p.CurrentStock, "pièce")} en stock",
                    $"produits?recherche={Uri.EscapeDataString(p.Reference)}")));

        await AjouterAsync(groupes, FamilleResultat.Matiere, "Matières premières",
            PermissionCodes.MatieresConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Materials.AsNoTracking(), balayage,
                        m => m.Name.ToLower().Contains(motif) || m.Reference.ToLower().Contains(motif))
                    .Select(m => new { m.Id, m.Name, m.Reference, m.CurrentQuantity })
                    .ToListAsync(cancellationToken))
                .Select(m => new Candidat(m.Id, m.Name, m.Reference,
                    $"{Formatage.Quantite(m.CurrentQuantity)} en stock",
                    $"matieres?recherche={Uri.EscapeDataString(m.Reference)}")));

        await AjouterAsync(groupes, FamilleResultat.Client, "Clients",
            PermissionCodes.ClientsConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Customers.AsNoTracking(), balayage,
                        c => c.FullName.ToLower().Contains(motif)
                                || c.CustomerNumber.ToLower().Contains(motif)
                                || (c.PhoneNumber != null && c.PhoneNumber.ToLower().Contains(motif)))
                    .Select(c => new { c.Id, c.FullName, c.CustomerNumber, c.PhoneNumber })
                    .ToListAsync(cancellationToken))
                .Select(c => new Candidat(c.Id, c.FullName, c.CustomerNumber, c.PhoneNumber,
                    $"clients?recherche={Uri.EscapeDataString(c.CustomerNumber)}")));

        await AjouterAsync(groupes, FamilleResultat.Fournisseur, "Fournisseurs",
            PermissionCodes.FournisseursConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Suppliers.AsNoTracking(), balayage,
                        f => f.Name.ToLower().Contains(motif)
                                || f.SupplierNumber.ToLower().Contains(motif)
                                || (f.PhoneNumber != null && f.PhoneNumber.ToLower().Contains(motif)))
                    .Select(f => new { f.Id, f.Name, f.SupplierNumber, f.PhoneNumber })
                    .ToListAsync(cancellationToken))
                .Select(f => new Candidat(f.Id, f.Name, f.SupplierNumber, f.PhoneNumber,
                    $"fournisseurs?recherche={Uri.EscapeDataString(f.SupplierNumber)}")));

        await AjouterAsync(groupes, FamilleResultat.OrdreProduction, "Ordres de production",
            PermissionCodes.ProductionConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.ProductionOrders.AsNoTracking(), balayage,
                        o => o.ProductionNumber.ToLower().Contains(motif)
                                || o.Product!.Name.ToLower().Contains(motif))
                    .Select(o => new { o.Id, o.ProductionNumber, Produit = o.Product!.Name, o.Status })
                    .ToListAsync(cancellationToken))
                .Select(o => new Candidat(o.Id, o.Produit, o.ProductionNumber, o.Status.Libelle(),
                    $"production?recherche={Uri.EscapeDataString(o.ProductionNumber)}")));

        await AjouterAsync(groupes, FamilleResultat.Commande, "Commandes personnalisées",
            PermissionCodes.CommandesConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.CustomOrders.AsNoTracking(), balayage,
                        c => c.OrderNumber.ToLower().Contains(motif)
                                || c.Customer!.FullName.ToLower().Contains(motif)
                                || (c.Description != null && c.Description.ToLower().Contains(motif)))
                    .Select(c => new { c.Id, c.OrderNumber, Client = c.Customer!.FullName, c.Status })
                    .ToListAsync(cancellationToken))
                .Select(c => new Candidat(c.Id, c.Client, c.OrderNumber, c.Status.Libelle(),
                    $"commandes?recherche={Uri.EscapeDataString(c.OrderNumber)}")));

        await AjouterAsync(groupes, FamilleResultat.Vente, "Ventes",
            PermissionCodes.VentesConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Sales.AsNoTracking(), balayage,
                        v => v.SaleNumber.ToLower().Contains(motif)
                                || (v.Customer != null && v.Customer.FullName.ToLower().Contains(motif)))
                    .Select(v => new { v.Id, v.SaleNumber, Client = v.Customer!.FullName, v.TotalAmount })
                    .ToListAsync(cancellationToken))
                .Select(v => new Candidat(v.Id, v.Client ?? "Client de passage", v.SaleNumber,
                    Formatage.Montant(v.TotalAmount),
                    $"ventes?recherche={Uri.EscapeDataString(v.SaleNumber)}")));

        await AjouterAsync(groupes, FamilleResultat.Facture, "Factures",
            PermissionCodes.FacturesConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Invoices.AsNoTracking(), balayage,
                        f => f.InvoiceNumber.ToLower().Contains(motif)
                                || (f.Customer != null && f.Customer.FullName.ToLower().Contains(motif)))
                    .Select(f => new { f.Id, f.InvoiceNumber, Client = f.Customer!.FullName, f.TotalAmount })
                    .ToListAsync(cancellationToken))
                .Select(f => new Candidat(f.Id, f.Client ?? "Client de passage", f.InvoiceNumber,
                    Formatage.Montant(f.TotalAmount), $"factures/{f.Id}")));

        await AjouterAsync(groupes, FamilleResultat.Achat, "Achats",
            PermissionCodes.AchatsConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Purchases.AsNoTracking(), balayage,
                        a => a.PurchaseNumber.ToLower().Contains(motif)
                                || a.Supplier!.Name.ToLower().Contains(motif))
                    .Select(a => new { a.Id, a.PurchaseNumber, Fournisseur = a.Supplier!.Name, a.TotalAmount })
                    .ToListAsync(cancellationToken))
                .Select(a => new Candidat(a.Id, a.Fournisseur, a.PurchaseNumber,
                    Formatage.Montant(a.TotalAmount),
                    $"achats?recherche={Uri.EscapeDataString(a.PurchaseNumber)}")));

        await AjouterAsync(groupes, FamilleResultat.Depense, "Dépenses",
            PermissionCodes.DepensesConsulter, recherche, maximum, async balayage =>
                (await Selectionner(_context.Expenses.AsNoTracking(), balayage,
                        d => d.Reference.ToLower().Contains(motif)
                                || d.Description.ToLower().Contains(motif)
                                || d.ExpenseCategory!.Name.ToLower().Contains(motif))
                    .Select(d => new { d.Id, d.Reference, d.Description, d.Amount })
                    .ToListAsync(cancellationToken))
                .Select(d => new Candidat(d.Id, d.Description, d.Reference,
                    Formatage.Montant(d.Amount),
                    $"depenses?recherche={Uri.EscapeDataString(d.Reference)}")));

        return new RechercheGlobaleDto(
            recherche,
            groupes.Sum(g => g.Resultats.Count),
            groupes.OrderByDescending(g => g.Resultats.Max(r => r.Pertinence)).ToList());
    }

    /// <summary>Fiche candidate avant classement par pertinence.</summary>
    private sealed record Candidat(int Id, string Titre, string Reference, string? Complement, string Adresse);

    /// <summary>
    /// Premier passage : les fiches dont le texte contient le terme cherché,
    /// sur toute la table. Second passage : les fiches les plus récentes, dont
    /// la ressemblance est ensuite mesurée en mémoire — c'est ce qui permet de
    /// retrouver « Émaillé » en tapant « emaille », ou un nom mal orthographié.
    /// </summary>
    private static IQueryable<T> Selectionner<T>(
        IQueryable<T> source, bool balayage, Expression<Func<T, bool>> filtre)
        where T : BaseEntity
        => balayage
            ? source.OrderByDescending(entite => entite.Id).Take(PlafondExamen)
            : source.Where(filtre).Take(PlafondExamen);

    private async Task AjouterAsync(
        List<GroupeResultatsDto> groupes,
        FamilleResultat famille,
        string libelle,
        string droit,
        string terme,
        int maximum,
        Func<bool, Task<IEnumerable<Candidat>>> lecture)
    {
        if (!_utilisateurCourant.PossedeDroit(droit))
        {
            return;
        }

        var resultats = Classer(famille, libelle, terme, maximum, await lecture(false));

        // Le terme est peut-être mal orthographié ou saisi sans accent : on
        // repasse alors sur les fiches récentes en tolérant les écarts.
        if (resultats.Count < maximum)
        {
            var complement = Classer(famille, libelle, terme, maximum, await lecture(true));

            resultats = resultats
                .Concat(complement.Where(c => resultats.All(r => r.Id != c.Id)))
                .OrderByDescending(resultat => resultat.Pertinence)
                .ThenBy(resultat => resultat.Titre)
                .Take(maximum)
                .ToList();
        }

        if (resultats.Count > 0)
        {
            groupes.Add(new GroupeResultatsDto(famille, libelle, resultats));
        }
    }

    private static List<ResultatRechercheDto> Classer(
        FamilleResultat famille, string libelle, string terme, int maximum, IEnumerable<Candidat> candidats)
        => candidats
            .Select(candidat => new ResultatRechercheDto(
                famille, libelle, candidat.Id, candidat.Titre, candidat.Reference, candidat.Complement,
                candidat.Adresse,
                Math.Max(Similitude.Noter(terme, candidat.Titre), Similitude.Noter(terme, candidat.Reference))))
            .Where(resultat => resultat.Pertinence >= Similitude.SeuilPertinence)
            .OrderByDescending(resultat => resultat.Pertinence)
            .ThenBy(resultat => resultat.Titre)
            .Take(maximum)
            .ToList();
}
