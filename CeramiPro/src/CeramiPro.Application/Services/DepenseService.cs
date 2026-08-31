using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Finances;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Expenses;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Dépenses de l'atelier : électricité, gaz, transport, salaires, équipement…
/// Une dépense supprimée reste consultable (règle métier n°15).
/// </summary>
public class DepenseService : IDepenseService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IServiceDateHeure _horloge;
    private readonly IAuditService _audit;

    public DepenseService(
        IApplicationDbContext context,
        IReferenceNumberService numerotation,
        IUtilisateurCourant utilisateurCourant,
        IServiceDateHeure horloge,
        IAuditService audit)
    {
        _context = context;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _horloge = horloge;
        _audit = audit;
    }

    public async Task<PagedResult<DepenseDto>> ListerAsync(
        FiltreDepensesRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();

        if (requete.CategorieId is not null)
        {
            requeteBase = requeteBase.Where(d => d.ExpenseCategoryId == requete.CategorieId);
        }

        if (requete.Du is not null)
        {
            var du = DateTime.SpecifyKind(requete.Du.Value.Date, DateTimeKind.Utc);
            requeteBase = requeteBase.Where(d => d.ExpenseDate >= du);
        }

        if (requete.Au is not null)
        {
            var au = DateTime.SpecifyKind(requete.Au.Value.Date.AddDays(1), DateTimeKind.Utc);
            requeteBase = requeteBase.Where(d => d.ExpenseDate < au);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(d =>
                d.Description.ToLower().Contains(recherche) ||
                d.Reference.ToLower().Contains(recherche) ||
                d.ExpenseCategory.Name.ToLower().Contains(recherche));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var depenses = await requeteBase
            .OrderByDescending(d => d.ExpenseDate).ThenByDescending(d => d.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<DepenseDto>(
            depenses.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<DepenseDto> CreerAsync(
        DepenseRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierAsync(requete, cancellationToken);

        var depense = new Expense
        {
            Reference = await _numerotation.GenererAsync(TypeDocument.Depense, cancellationToken),
            ExpenseCategoryId = requete.CategorieId,
            Amount = requete.Montant,
            ExpenseDate = requete.Date ?? _horloge.MaintenantUtc,
            Description = requete.Description.Trim(),
            ReceiptPath = Nettoyer(requete.Justificatif),
            PaymentMethodId = requete.ModeReglementId,
            UserId = _utilisateurCourant.UtilisateurId
        };

        _context.Expenses.Add(depense);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Expense), depense.Id.ToString(),
            $"Dépense {depense.Reference} de {Formatage.Montant(depense.Amount)}.",
            null, cancellationToken);

        return await ObtenirAsync(depense.Id, cancellationToken);
    }

    public async Task<DepenseDto> ModifierAsync(
        int id, DepenseRequete requete, CancellationToken cancellationToken = default)
    {
        var depense = await _context.Expenses.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Dépense", id);

        await VerifierAsync(requete, cancellationToken);

        depense.ExpenseCategoryId = requete.CategorieId;
        depense.Amount = requete.Montant;
        depense.ExpenseDate = requete.Date ?? depense.ExpenseDate;
        depense.Description = requete.Description.Trim();
        depense.ReceiptPath = Nettoyer(requete.Justificatif);
        depense.PaymentMethodId = requete.ModeReglementId;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Expense), id.ToString(),
            $"Modification de la dépense {depense.Reference}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task SupprimerAsync(int id, string motif, CancellationToken cancellationToken = default)
    {
        var depense = await _context.Expenses.FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Dépense", id);

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new RegleMetierException("Indiquez le motif de la suppression.");
        }

        depense.Description = $"{depense.Description} — supprimée : {motif.Trim()}";

        // Suppression logique : l'écriture reste dans l'historique comptable.
        _context.Expenses.Remove(depense);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Expense), id.ToString(),
            $"Suppression de la dépense {depense.Reference} : {motif.Trim()}", null, cancellationToken);
    }

    public async Task<decimal> TotalAsync(
        DateTime du, DateTime au, CancellationToken cancellationToken = default)
    {
        var debut = DateTime.SpecifyKind(du.Date, DateTimeKind.Utc);
        var fin = DateTime.SpecifyKind(au.Date.AddDays(1), DateTimeKind.Utc);

        return await _context.Expenses
            .Where(d => d.ExpenseDate >= debut && d.ExpenseDate < fin)
            .SumAsync(d => (decimal?)d.Amount, cancellationToken) ?? 0m;
    }

    private async Task<DepenseDto> ObtenirAsync(int id, CancellationToken cancellationToken)
    {
        var depense = await ChargerAvecDetails().AsNoTracking()
                          .FirstOrDefaultAsync(d => d.Id == id, cancellationToken)
                      ?? throw IntrouvableException.Pour("Dépense", id);

        return Convertir(depense);
    }

    private IQueryable<Expense> ChargerAvecDetails()
        => _context.Expenses
            .Include(d => d.ExpenseCategory)
            .Include(d => d.PaymentMethod)
            .Include(d => d.User);

    private async Task VerifierAsync(DepenseRequete requete, CancellationToken cancellationToken)
    {
        if (requete.Montant <= 0)
        {
            throw new RegleMetierException("Le montant de la dépense doit être supérieur à zéro.");
        }

        if (string.IsNullOrWhiteSpace(requete.Description))
        {
            throw new RegleMetierException("Décrivez la dépense en quelques mots.");
        }

        if (!await _context.ExpenseCategories.AnyAsync(c => c.Id == requete.CategorieId, cancellationToken))
        {
            throw new RegleMetierException("La catégorie de dépense sélectionnée n'existe pas.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private static DepenseDto Convertir(Expense d) => new(
        d.Id, d.Reference, d.ExpenseCategoryId, d.ExpenseCategory.Name, d.Amount,
        d.ExpenseDate, d.Description, d.ReceiptPath, d.PaymentMethodId,
        d.PaymentMethod?.Name, d.User?.FullName);
}
