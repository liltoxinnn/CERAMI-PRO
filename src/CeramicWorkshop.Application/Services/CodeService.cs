using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Codes;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Étiquettes des produits et lecture des codes scannés.
///
/// Chaque fiche de l'atelier porte un numéro unique (PRD-…, ORD-…, CMD-…) :
/// scanner ce numéro, à la douchette ou à la caméra, ouvre directement l'écran
/// correspondant.
/// </summary>
public class CodeService : ICodeService
{
    /// <summary>Nombre maximal d'étiquettes imprimées en une seule planche.</summary>
    public const int EtiquettesMaximum = 200;

    private readonly IApplicationDbContext _context;
    private readonly ICodeGraphiqueService _images;
    private readonly ICurrentUserService _utilisateurCourant;

    public CodeService(
        IApplicationDbContext context,
        ICodeGraphiqueService images,
        ICurrentUserService utilisateurCourant)
    {
        _context = context;
        _images = images;
        _utilisateurCourant = utilisateurCourant;
    }

    public async Task<EtiquetteDto> EtiquetteProduitAsync(
        int produitId, CancellationToken cancellationToken = default)
    {
        var produit = await _context.Products
                          .Include(p => p.ProductCategory)
                          .AsNoTracking()
                          .FirstOrDefaultAsync(p => p.Id == produitId, cancellationToken)
                      ?? throw NotFoundException.Pour("Produit", produitId);

        return Construire(produit);
    }

    public async Task<IReadOnlyList<EtiquetteDto>> EtiquettesAsync(
        EtiquettesRequete requete, CancellationToken cancellationToken = default)
    {
        var identifiants = requete.ProduitIds.Distinct().ToList();

        if (identifiants.Count == 0)
        {
            throw new BusinessRuleException("Choisissez au moins un produit à étiqueter.");
        }

        var exemplaires = Math.Max(1, requete.Exemplaires);

        if (identifiants.Count * exemplaires > EtiquettesMaximum)
        {
            throw new BusinessRuleException(
                $"Une planche ne peut pas dépasser {EtiquettesMaximum} étiquettes. " +
                "Réduisez le nombre de produits ou d'exemplaires.");
        }

        var produits = await _context.Products
            .Include(p => p.ProductCategory)
            .AsNoTracking()
            .Where(p => identifiants.Contains(p.Id))
            .OrderBy(p => p.Name)
            .ToListAsync(cancellationToken);

        var etiquettes = new List<EtiquetteDto>();

        foreach (var produit in produits)
        {
            var etiquette = Construire(produit);

            for (var exemplaire = 0; exemplaire < exemplaires; exemplaire++)
            {
                etiquettes.Add(etiquette);
            }
        }

        return etiquettes;
    }

    public async Task<ResultatScanDto> ResoudreAsync(
        string code, CancellationToken cancellationToken = default)
    {
        var recherche = (code ?? string.Empty).Trim();

        if (recherche.Length == 0)
        {
            return new ResultatScanDto(
                false, CibleScan.Inconnu, null, string.Empty, "Aucun code lu.", null, null);
        }

        var normalise = recherche.ToLower();

        var produit = !PeutConsulter(PermissionCodes.ProduitsConsulter) ? null
            : await _context.Products.AsNoTracking()
            .Where(p => p.Reference.ToLower() == normalise
                        || p.Barcode!.ToLower() == normalise
                        || p.QrCode!.ToLower() == normalise)
            .Select(p => new { p.Id, p.Name, p.Reference, p.CurrentStock, p.SellingPrice })
            .FirstOrDefaultAsync(cancellationToken);

        if (produit is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.Produit, produit.Id, produit.Reference, produit.Name,
                $"{MontantFormatter.FormaterQuantite(produit.CurrentStock, "pièce")} en stock — " +
                $"{MontantFormatter.Formater(produit.SellingPrice)}",
                $"produits?recherche={Uri.EscapeDataString(produit.Reference)}");
        }

        var matiere = !PeutConsulter(PermissionCodes.MatieresConsulter) ? null
            : await _context.Materials.AsNoTracking()
            .Where(m => m.Reference.ToLower() == normalise)
            .Select(m => new { m.Id, m.Name, m.Reference, m.CurrentQuantity })
            .FirstOrDefaultAsync(cancellationToken);

        if (matiere is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.Matiere, matiere.Id, matiere.Reference, matiere.Name,
                $"{MontantFormatter.FormaterQuantite(matiere.CurrentQuantity)} en stock",
                $"matieres?recherche={Uri.EscapeDataString(matiere.Reference)}");
        }

        var production = !PeutConsulter(PermissionCodes.ProductionConsulter) ? null
            : await _context.ProductionOrders.AsNoTracking()
            .Where(o => o.ProductionNumber.ToLower() == normalise)
            .Select(o => new { o.Id, o.ProductionNumber, Produit = o.Product!.Name, o.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (production is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.OrdreProduction, production.Id, production.ProductionNumber,
                $"Ordre de production — {production.Produit}",
                production.Status.Libelle(),
                $"production?recherche={Uri.EscapeDataString(production.ProductionNumber)}");
        }

        var commande = !PeutConsulter(PermissionCodes.CommandesConsulter) ? null
            : await _context.CustomOrders.AsNoTracking()
            .Where(c => c.OrderNumber.ToLower() == normalise)
            .Select(c => new { c.Id, c.OrderNumber, Client = c.Customer!.FullName, c.Status })
            .FirstOrDefaultAsync(cancellationToken);

        if (commande is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.Commande, commande.Id, commande.OrderNumber,
                $"Commande — {commande.Client}", commande.Status.Libelle(),
                $"commandes?recherche={Uri.EscapeDataString(commande.OrderNumber)}");
        }

        var vente = !PeutConsulter(PermissionCodes.VentesConsulter) ? null
            : await _context.Sales.AsNoTracking()
            .Where(v => v.SaleNumber.ToLower() == normalise)
            .Select(v => new { v.Id, v.SaleNumber, v.TotalAmount })
            .FirstOrDefaultAsync(cancellationToken);

        if (vente is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.Vente, vente.Id, vente.SaleNumber, "Vente",
                MontantFormatter.Formater(vente.TotalAmount),
                $"ventes?recherche={Uri.EscapeDataString(vente.SaleNumber)}");
        }

        var facture = !PeutConsulter(PermissionCodes.FacturesConsulter) ? null
            : await _context.Invoices.AsNoTracking()
            .Where(f => f.InvoiceNumber.ToLower() == normalise)
            .Select(f => new { f.Id, f.InvoiceNumber, f.TotalAmount })
            .FirstOrDefaultAsync(cancellationToken);

        if (facture is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.Facture, facture.Id, facture.InvoiceNumber, "Facture",
                MontantFormatter.Formater(facture.TotalAmount),
                $"factures/{facture.Id}");
        }

        var achat = !PeutConsulter(PermissionCodes.AchatsConsulter) ? null
            : await _context.Purchases.AsNoTracking()
            .Where(a => a.PurchaseNumber.ToLower() == normalise)
            .Select(a => new { a.Id, a.PurchaseNumber, Fournisseur = a.Supplier!.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (achat is not null)
        {
            return new ResultatScanDto(
                true, CibleScan.Achat, achat.Id, achat.PurchaseNumber,
                $"Achat — {achat.Fournisseur}", null,
                $"achats?recherche={Uri.EscapeDataString(achat.PurchaseNumber)}");
        }

        return new ResultatScanDto(
            false, CibleScan.Inconnu, null, recherche,
            "Aucune fiche ne porte ce code.",
            "Vérifiez l'étiquette ou saisissez le numéro à la main.", null);
    }

    /// <summary>
    /// Le scanner ne cherche que dans les modules que l'utilisateur a le droit
    /// de consulter : un caissier ne découvre pas les achats fournisseurs en
    /// passant une étiquette devant la douchette.
    /// </summary>
    private bool PeutConsulter(string droit) => _utilisateurCourant.PossedeDroit(droit);

    private EtiquetteDto Construire(Domain.Entities.Catalog.Product produit)
    {
        var codeBarres = string.IsNullOrWhiteSpace(produit.Barcode) ? produit.Reference : produit.Barcode;
        var codeQr = string.IsNullOrWhiteSpace(produit.QrCode) ? produit.Reference : produit.QrCode;

        return new EtiquetteDto(
            produit.Id,
            produit.Name,
            produit.Reference,
            produit.ProductCategory?.Name ?? "—",
            produit.SellingPrice,
            MontantFormatter.Formater(produit.SellingPrice),
            codeBarres,
            codeQr,
            _images.CodeBarresEnSvg(codeBarres),
            _images.QrEnSvg(codeQr));
    }
}
