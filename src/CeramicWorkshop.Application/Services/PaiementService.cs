using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Payments;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Encaissements clients : paiement complet, partiel, acompte ou règlement de
/// dette. Chaque paiement est enregistré individuellement (règle n°14), le reste
/// à payer est recalculé (règle n°13) et rien n'est jamais supprimé
/// physiquement (règle n°15).
/// </summary>
public class PaiementService : IPaiementService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;

    public PaiementService(
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

    public async Task<PagedResult<PaiementDto>> ListerAsync(
        FiltrePaiementsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.ClientId is not null)
        {
            requeteBase = requeteBase.Where(p => p.CustomerId == requete.ClientId);
        }

        if (requete.Du is not null)
        {
            var du = DateTime.SpecifyKind(requete.Du.Value.Date, DateTimeKind.Utc);
            requeteBase = requeteBase.Where(p => p.PaymentDate >= du);
        }

        if (requete.Au is not null)
        {
            var au = DateTime.SpecifyKind(requete.Au.Value.Date.AddDays(1), DateTimeKind.Utc);
            requeteBase = requeteBase.Where(p => p.PaymentDate < au);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(p =>
                p.PaymentNumber.ToLower().Contains(recherche) ||
                (p.Customer != null && p.Customer.FullName.ToLower().Contains(recherche)) ||
                (p.Reference != null && p.Reference.ToLower().Contains(recherche)));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var paiements = await requeteBase
            .OrderByDescending(p => p.PaymentDate).ThenByDescending(p => p.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<PaiementDto>(
            paiements.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<PaiementDto> EnregistrerAsync(
        PaiementRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.Montant <= 0)
        {
            throw new BusinessRuleException("Le montant du paiement doit être supérieur à zéro.");
        }

        if (!await _context.PaymentMethods.AnyAsync(m => m.Id == requete.ModeReglementId, cancellationToken))
        {
            throw new BusinessRuleException("Le mode de règlement sélectionné n'existe pas.");
        }

        if (requete.VenteId is null && requete.CommandeId is null
            && requete.FactureId is null && requete.ClientId is null)
        {
            throw new BusinessRuleException("Indiquez le client, la vente ou la commande concernée.");
        }

        var clientId = requete.ClientId;

        var paiement = new Payment
        {
            PaymentNumber = await _numerotation.GenererAsync(TypeDocument.Paiement, cancellationToken),
            Direction = PaymentDirection.Encaissement,
            Amount = requete.Montant,
            PaymentDate = requete.Date ?? _horloge.UtcNow,
            PaymentMethodId = requete.ModeReglementId,
            IsDeposit = requete.Acompte,
            Reference = Nettoyer(requete.Reference),
            Notes = Nettoyer(requete.Notes),
            UserId = _utilisateurCourant.UserId
        };

        // Le paiement met à jour le document qu'il règle.
        if (requete.VenteId is not null)
        {
            var vente = await _context.Sales.FirstOrDefaultAsync(v => v.Id == requete.VenteId, cancellationToken)
                        ?? throw NotFoundException.Pour("Vente", requete.VenteId);

            if (vente.Status == SaleStatus.Annulee)
            {
                throw new BusinessRuleException($"La vente {vente.SaleNumber} est annulée.");
            }

            VerifierMontant(requete.Montant, vente.RemainingAmount, $"la vente {vente.SaleNumber}");

            vente.PaidAmount += requete.Montant;
            paiement.SaleId = vente.Id;
            clientId ??= vente.CustomerId;
        }

        if (requete.CommandeId is not null)
        {
            var commande = await _context.CustomOrders
                               .FirstOrDefaultAsync(c => c.Id == requete.CommandeId, cancellationToken)
                           ?? throw NotFoundException.Pour("Commande personnalisée", requete.CommandeId);

            if (commande.Status == CustomOrderStatus.Annule)
            {
                throw new BusinessRuleException($"La commande {commande.OrderNumber} est annulée.");
            }

            VerifierMontant(requete.Montant, commande.RemainingAmount, $"la commande {commande.OrderNumber}");

            commande.PaidAmount += requete.Montant;
            paiement.CustomOrderId = commande.Id;
            clientId ??= commande.CustomerId;
        }

        var factureId = requete.FactureId
                        ?? await TrouverFactureAsync(requete.VenteId, requete.CommandeId, cancellationToken);

        if (factureId is not null)
        {
            var facture = await _context.Invoices.FirstOrDefaultAsync(f => f.Id == factureId, cancellationToken);

            if (facture is not null && facture.Status != InvoiceStatus.Annulee)
            {
                facture.PaidAmount = Math.Min(facture.TotalAmount, facture.PaidAmount + requete.Montant);
                facture.Status = facture.PaidAmount >= facture.TotalAmount
                    ? InvoiceStatus.Payee
                    : InvoiceStatus.PartiellementPayee;

                paiement.InvoiceId = facture.Id;
                clientId ??= facture.CustomerId;
            }
        }

        paiement.CustomerId = clientId;

        _context.Payments.Add(paiement);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Payment), paiement.Id.ToString(),
            $"Encaissement {paiement.PaymentNumber} de {MontantFormatter.Formater(requete.Montant)}" +
            (requete.Acompte ? " (acompte)." : "."), null, cancellationToken);

        var enregistre = await ChargerAvecDetails().AsNoTracking()
            .FirstAsync(p => p.Id == paiement.Id, cancellationToken);

        return Convertir(enregistre);
    }

    public async Task AnnulerAsync(int id, string motif, CancellationToken cancellationToken = default)
    {
        var paiement = await _context.Payments.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Paiement", id);

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new BusinessRuleException("Indiquez le motif de l'annulation.");
        }

        // Les soldes des documents réglés sont corrigés.
        if (paiement.SaleId is not null)
        {
            var vente = await _context.Sales.FirstOrDefaultAsync(v => v.Id == paiement.SaleId, cancellationToken);
            if (vente is not null)
            {
                vente.PaidAmount = Math.Max(0m, vente.PaidAmount - paiement.Amount);
            }
        }

        if (paiement.CustomOrderId is not null)
        {
            var commande = await _context.CustomOrders
                .FirstOrDefaultAsync(c => c.Id == paiement.CustomOrderId, cancellationToken);
            if (commande is not null)
            {
                commande.PaidAmount = Math.Max(0m, commande.PaidAmount - paiement.Amount);
            }
        }

        if (paiement.InvoiceId is not null)
        {
            var facture = await _context.Invoices
                .FirstOrDefaultAsync(f => f.Id == paiement.InvoiceId, cancellationToken);

            if (facture is not null)
            {
                facture.PaidAmount = Math.Max(0m, facture.PaidAmount - paiement.Amount);
                facture.Status = facture.PaidAmount <= 0
                    ? InvoiceStatus.Emise
                    : facture.PaidAmount >= facture.TotalAmount
                        ? InvoiceStatus.Payee
                        : InvoiceStatus.PartiellementPayee;
            }
        }

        paiement.Notes = string.IsNullOrWhiteSpace(paiement.Notes)
            ? $"Annulé : {motif.Trim()}"
            : $"{paiement.Notes}\nAnnulé : {motif.Trim()}";

        // Suppression logique : l'écriture reste consultable (règle métier n°15).
        _context.Payments.Remove(paiement);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Annulation, nameof(Payment), id.ToString(),
            $"Annulation du paiement {paiement.PaymentNumber} " +
            $"({MontantFormatter.Formater(paiement.Amount)}) : {motif.Trim()}", null, cancellationToken);
    }

    private async Task<int?> TrouverFactureAsync(
        int? venteId, int? commandeId, CancellationToken cancellationToken)
    {
        if (venteId is null && commandeId is null)
        {
            return null;
        }

        return await _context.Invoices
            .Where(f => f.Status != InvoiceStatus.Annulee
                        && ((venteId != null && f.SaleId == venteId)
                            || (commandeId != null && f.CustomOrderId == commandeId)))
            .Select(f => (int?)f.Id)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private static void VerifierMontant(decimal montant, decimal reste, string document)
    {
        if (montant > reste)
        {
            throw new BusinessRuleException(
                $"Le montant dépasse le reste à payer sur {document} " +
                $"({MontantFormatter.Formater(reste)}).");
        }
    }

    private IQueryable<Payment> ChargerAvecDetails()
        => _context.Payments
            .Include(p => p.Customer)
            .Include(p => p.Sale)
            .Include(p => p.CustomOrder)
            .Include(p => p.Invoice)
            .Include(p => p.PaymentMethod)
            .Include(p => p.User);

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static PaiementDto Convertir(Payment p) => new(
        p.Id,
        p.PaymentNumber,
        p.CustomerId,
        p.Customer?.FullName,
        p.SaleId,
        p.Sale?.SaleNumber,
        p.CustomOrderId,
        p.CustomOrder?.OrderNumber,
        p.InvoiceId,
        p.Invoice?.InvoiceNumber,
        p.Amount,
        p.PaymentDate,
        p.PaymentMethodId,
        p.PaymentMethod.Name,
        p.IsDeposit,
        p.Reference,
        p.Notes,
        p.User?.FullName);
}
