using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Stock;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Entities.Suppliers;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Fournisseurs de l'atelier : coordonnées, historique d'achats et dettes.
/// Le montant restant dû est toujours recalculé à partir des achats confirmés
/// et des règlements enregistrés.
/// </summary>
public class FournisseurService : IFournisseurService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;

    public FournisseurService(
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

    public async Task<PagedResult<FournisseurDto>> ListerAsync(
        FiltreFournisseursRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.Suppliers.AsNoTracking().AsQueryable();

        if (!requete.InclureInactifs)
        {
            requeteBase = requeteBase.Where(f => f.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(f =>
                f.Name.ToLower().Contains(recherche) ||
                f.SupplierNumber.ToLower().Contains(recherche) ||
                (f.CompanyName != null && f.CompanyName.ToLower().Contains(recherche)) ||
                (f.PhoneNumber != null && f.PhoneNumber.Contains(recherche)));
        }

        var elements = await requeteBase.Select(Projeter()).ToListAsync(cancellationToken);

        if (requete.SeulementAvecDette)
        {
            elements = elements.Where(f => f.Reste > 0).ToList();
        }

        var total = elements.Count;

        var page = elements
            .OrderBy(f => f.Nom)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToList();

        return new PagedResult<FournisseurDto>(page, total, requete.Page, requete.TaillePage);
    }

    public async Task<FournisseurDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Suppliers.AsNoTracking().Where(f => f.Id == id)
               .Select(Projeter()).FirstOrDefaultAsync(cancellationToken)
           ?? throw NotFoundException.Pour("Fournisseur", id);

    public async Task<FournisseurDto> CreerAsync(
        FournisseurRequete requete, CancellationToken cancellationToken = default)
    {
        var fournisseur = new Supplier
        {
            SupplierNumber = await _numerotation.GenererAsync(TypeDocument.Fournisseur, cancellationToken),
            Name = requete.Nom.Trim(),
            CompanyName = Nettoyer(requete.Entreprise),
            PhoneNumber = Nettoyer(requete.Telephone),
            Email = Nettoyer(requete.Email),
            Address = Nettoyer(requete.Adresse),
            City = Nettoyer(requete.Ville),
            Notes = Nettoyer(requete.Notes),
            IsActive = requete.Actif
        };

        _context.Suppliers.Add(fournisseur);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Supplier), fournisseur.Id.ToString(),
            $"Création du fournisseur « {fournisseur.Name} ».", null, cancellationToken);

        return await ObtenirAsync(fournisseur.Id, cancellationToken);
    }

    public async Task<FournisseurDto> ModifierAsync(
        int id, FournisseurRequete requete, CancellationToken cancellationToken = default)
    {
        var fournisseur = await _context.Suppliers.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                          ?? throw NotFoundException.Pour("Fournisseur", id);

        fournisseur.Name = requete.Nom.Trim();
        fournisseur.CompanyName = Nettoyer(requete.Entreprise);
        fournisseur.PhoneNumber = Nettoyer(requete.Telephone);
        fournisseur.Email = Nettoyer(requete.Email);
        fournisseur.Address = Nettoyer(requete.Adresse);
        fournisseur.City = Nettoyer(requete.Ville);
        fournisseur.Notes = Nettoyer(requete.Notes);
        fournisseur.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Supplier), id.ToString(),
            $"Modification du fournisseur « {fournisseur.Name} ».", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var fournisseur = await _context.Suppliers.FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
                          ?? throw NotFoundException.Pour("Fournisseur", id);

        var utilise = await _context.Purchases.IgnoreQueryFilters().AnyAsync(a => a.SupplierId == id, cancellationToken)
                      || await _context.Materials.AnyAsync(m => m.SupplierId == id, cancellationToken);

        if (utilise)
        {
            throw new BusinessRuleException(
                $"Le fournisseur « {fournisseur.Name} » possède un historique. " +
                "Désactivez-le au lieu de le supprimer.");
        }

        _context.Suppliers.Remove(fournisseur);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Supplier), id.ToString(),
            $"Suppression du fournisseur « {fournisseur.Name} ».", null, cancellationToken);
    }

    public async Task<IReadOnlyList<ReglementFournisseurDto>> ListerReglementsAsync(
        int fournisseurId, CancellationToken cancellationToken = default)
        => await _context.SupplierPayments
            .Include(r => r.Supplier).Include(r => r.Purchase).Include(r => r.PaymentMethod).Include(r => r.User)
            .AsNoTracking()
            .Where(r => r.SupplierId == fournisseurId)
            .OrderByDescending(r => r.PaymentDate)
            .Select(r => new ReglementFournisseurDto(
                r.Id, r.PaymentNumber, r.SupplierId, r.Supplier.Name,
                r.PurchaseId, r.Purchase != null ? r.Purchase.PurchaseNumber : null,
                r.Amount, r.PaymentDate, r.PaymentMethodId, r.PaymentMethod.Name,
                r.Reference, r.Notes, r.User != null ? r.User.FullName : null))
            .ToListAsync(cancellationToken);

    public async Task<ReglementFournisseurDto> EnregistrerReglementAsync(
        ReglementFournisseurRequete requete, CancellationToken cancellationToken = default)
    {
        if (requete.Montant <= 0)
        {
            throw new BusinessRuleException("Le montant du règlement doit être supérieur à zéro.");
        }

        var fournisseur = await _context.Suppliers
                              .FirstOrDefaultAsync(f => f.Id == requete.FournisseurId, cancellationToken)
                          ?? throw NotFoundException.Pour("Fournisseur", requete.FournisseurId);

        if (!await _context.PaymentMethods.AnyAsync(m => m.Id == requete.ModeReglementId, cancellationToken))
        {
            throw new BusinessRuleException("Le mode de règlement sélectionné n'existe pas.");
        }

        Domain.Entities.Purchasing.Purchase? achat = null;

        if (requete.AchatId is not null)
        {
            achat = await _context.Purchases
                        .FirstOrDefaultAsync(a => a.Id == requete.AchatId, cancellationToken)
                    ?? throw NotFoundException.Pour("Achat", requete.AchatId);

            if (achat.SupplierId != fournisseur.Id)
            {
                throw new BusinessRuleException("Cet achat n'appartient pas au fournisseur sélectionné.");
            }

            if (requete.Montant > achat.TotalAmount - achat.PaidAmount)
            {
                throw new BusinessRuleException(
                    $"Le montant dépasse le reste dû sur l'achat {achat.PurchaseNumber} " +
                    $"({MontantFormatter.Formater(achat.TotalAmount - achat.PaidAmount)}).");
            }

            achat.PaidAmount += requete.Montant;
        }

        var reglement = new SupplierPayment
        {
            PaymentNumber = await _numerotation.GenererAsync(TypeDocument.ReglementFournisseur, cancellationToken),
            SupplierId = fournisseur.Id,
            PurchaseId = achat?.Id,
            Amount = requete.Montant,
            PaymentDate = requete.Date ?? _horloge.UtcNow,
            PaymentMethodId = requete.ModeReglementId,
            Reference = Nettoyer(requete.Reference),
            Notes = Nettoyer(requete.Notes),
            UserId = _utilisateurCourant.UserId
        };

        _context.SupplierPayments.Add(reglement);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(SupplierPayment), reglement.Id.ToString(),
            $"Règlement de {MontantFormatter.Formater(requete.Montant)} au fournisseur « {fournisseur.Name} ».",
            null, cancellationToken);

        return (await ListerReglementsAsync(fournisseur.Id, cancellationToken)).First(r => r.Id == reglement.Id);
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    /// <summary>Projection incluant les totaux d'achats et de règlements.</summary>
    private static System.Linq.Expressions.Expression<Func<Supplier, FournisseurDto>> Projeter()
        => f => new FournisseurDto(
            f.Id,
            f.SupplierNumber,
            f.Name,
            f.CompanyName,
            f.PhoneNumber,
            f.Email,
            f.Address,
            f.City,
            f.Notes,
            f.IsActive,
            f.Purchases.Where(a => a.Status != PurchaseStatus.Brouillon
                                   && a.Status != PurchaseStatus.Annule).Sum(a => (decimal?)a.TotalAmount) ?? 0m,
            f.SupplierPayments.Sum(r => (decimal?)r.Amount) ?? 0m,
            (f.Purchases.Where(a => a.Status != PurchaseStatus.Brouillon
                                    && a.Status != PurchaseStatus.Annule).Sum(a => (decimal?)a.TotalAmount) ?? 0m)
            - (f.SupplierPayments.Sum(r => (decimal?)r.Amount) ?? 0m),
            f.Materials.Count,
            f.Purchases.Where(a => a.Status != PurchaseStatus.Annule)
                .Max(a => (DateTime?)a.PurchaseDate));
}
