using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Invoicing;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Factures clients. Une facture est émise automatiquement à chaque vente ;
/// elle peut aussi être créée pour une commande personnalisée.
/// </summary>
public class FactureService : IFactureService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;

    public FactureService(
        IApplicationDbContext context,
        IReferenceNumberService numerotation,
        ICurrentUserService utilisateurCourant,
        IDateTimeService horloge,
        IAuditService audit)
    {
        _context = context;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<FactureDto>> ListerAsync(
        FiltreFacturesRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.ClientId is not null)
        {
            requeteBase = requeteBase.Where(f => f.CustomerId == requete.ClientId);
        }

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(f => f.Status == requete.Statut);
        }

        if (requete.SeulementImpayees)
        {
            requeteBase = requeteBase.Where(f =>
                f.PaidAmount < f.TotalAmount && f.Status != InvoiceStatus.Annulee);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(f =>
                f.InvoiceNumber.ToLower().Contains(recherche) ||
                (f.Customer != null && f.Customer.FullName.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var factures = await requeteBase
            .OrderByDescending(f => f.IssueDate).ThenByDescending(f => f.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<FactureDto>(
            factures.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<FactureDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var facture = await ChargerAvecDetails().AsNoTracking()
                          .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                      ?? throw NotFoundException.Pour("Facture", id);

        return Convertir(facture);
    }

    public async Task<FactureDto> EmettrePourCommandeAsync(
        FactureCommandeRequete requete, CancellationToken cancellationToken = default)
    {
        var commande = await _context.CustomOrders
                           .Include(c => c.Customer)
                           .FirstOrDefaultAsync(c => c.Id == requete.CommandeId, cancellationToken)
                       ?? throw NotFoundException.Pour("Commande personnalisée", requete.CommandeId);

        if (commande.Status == CustomOrderStatus.Annule)
        {
            throw new BusinessRuleException($"La commande {commande.OrderNumber} est annulée.");
        }

        var existante = await _context.Invoices
            .FirstOrDefaultAsync(f => f.CustomOrderId == commande.Id
                                      && f.Status != InvoiceStatus.Annulee, cancellationToken);

        if (existante is not null)
        {
            throw new BusinessRuleException(
                $"La commande {commande.OrderNumber} possède déjà la facture {existante.InvoiceNumber}.");
        }

        var parametres = await _context.BusinessSettings.OrderBy(p => p.Id).FirstAsync(cancellationToken);

        var sousTotal = Math.Round(commande.Quantity * commande.UnitPrice, 2);
        var tva = parametres.TaxEnabled
            ? Math.Round((sousTotal - commande.DiscountAmount) * parametres.DefaultTaxRate / 100m, 2)
            : 0m;

        var facture = new Invoice
        {
            InvoiceNumber = await _numerotation.GenererAsync(TypeDocument.Facture, cancellationToken),
            CustomerId = commande.CustomerId,
            CustomOrderId = commande.Id,
            IssueDate = _horloge.UtcNow,
            DueDate = requete.DateEcheance ?? commande.Deadline,
            Subtotal = sousTotal,
            DiscountAmount = commande.DiscountAmount,
            TaxRate = parametres.TaxEnabled ? parametres.DefaultTaxRate : 0m,
            TaxAmount = tva,
            TotalAmount = Math.Round(sousTotal - commande.DiscountAmount + tva, 2),
            PaidAmount = commande.PaidAmount,
            Notes = Nettoyer(requete.Notes),
            UserId = _utilisateurCourant.UserId
        };

        facture.Status = facture.PaidAmount >= facture.TotalAmount
            ? InvoiceStatus.Payee
            : facture.PaidAmount > 0
                ? InvoiceStatus.PartiellementPayee
                : InvoiceStatus.Emise;

        facture.Items.Add(new InvoiceItem
        {
            Description = commande.Title,
            Quantity = commande.Quantity,
            UnitPrice = commande.UnitPrice,
            DiscountAmount = commande.DiscountAmount,
            LineTotal = Math.Round(sousTotal - commande.DiscountAmount, 2)
        });

        _context.Invoices.Add(facture);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Invoice), facture.Id.ToString(),
            $"Émission de la facture {facture.InvoiceNumber} pour la commande {commande.OrderNumber}.",
            null, cancellationToken);

        return await ObtenirAsync(facture.Id, cancellationToken);
    }

    private IQueryable<Invoice> ChargerAvecDetails()
        => _context.Invoices
            .Include(f => f.Customer)
            .Include(f => f.Sale)
            .Include(f => f.CustomOrder)
            .Include(f => f.Items);

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static FactureDto Convertir(Invoice f) => new(
        f.Id,
        f.InvoiceNumber,
        f.CustomerId,
        f.Customer?.FullName ?? "Client de passage",
        f.SaleId,
        f.Sale?.SaleNumber,
        f.CustomOrderId,
        f.CustomOrder?.OrderNumber,
        f.IssueDate,
        f.DueDate,
        f.Subtotal,
        f.DiscountAmount,
        f.TaxRate,
        f.TaxAmount,
        f.TotalAmount,
        f.PaidAmount,
        f.RemainingAmount,
        f.Status,
        f.Status.Libelle(),
        f.Notes,
        f.Items.Select(i => new LigneFactureDto(
            i.Id, i.ProductId, i.Description, i.Quantity,
            i.UnitPrice, i.DiscountAmount, i.LineTotal)).ToList());
}
