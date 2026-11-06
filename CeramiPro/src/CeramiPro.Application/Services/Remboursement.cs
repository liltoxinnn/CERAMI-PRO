using System.Linq.Expressions;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Payments;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Remboursement d'un client à l'annulation d'une vente ou d'une commande.
///
/// Les paiements ne sont pas effacés : ils sont annulés (suppression
/// logique), avec le motif, et disparaissent des encaissements. L'appelant
/// enregistre le tout en une seule fois, avec l'annulation elle-même.
/// </summary>
internal static class Remboursement
{
    public static async Task AnnulerPaiementsAsync(
        IApplicationDbContext context, Expression<Func<Payment, bool>> paiementsDuDocument, string motif,
        CancellationToken cancellationToken)
    {
        var paiements = await context.Payments.Where(paiementsDuDocument).ToListAsync(cancellationToken);

        foreach (var paiement in paiements)
        {
            paiement.Notes = string.IsNullOrWhiteSpace(paiement.Notes) ? motif : $"{paiement.Notes}\n{motif}";
            paiement.DeletionReason = motif;
            context.Payments.Remove(paiement);
        }
    }
}
