using CeramiPro.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Common;

/// <summary>
/// Une opération de l'atelier s'enregistre en entier, ou pas du tout.
///
/// Une vente écrit son en-tête, ses lignes, un mouvement de stock par
/// produit et son règlement. Si le deuxième produit manque en stock, rien ne
/// doit rester du premier : ni vente sans sortie de stock, ni stock diminué
/// sans vente. La transaction le garantit côté base ; vider le suivi des
/// entités le garantit côté logiciel, pour que l'opération suivante ne
/// réécrive pas en silence ce qui vient d'échouer.
/// </summary>
public static class TransactionMetier
{
    public static async Task<T> EnTransactionAsync<T>(
        this IApplicationDbContext contexte, Func<Task<T>> operation, CancellationToken cancellationToken = default)
    {
        var bdd = contexte.Database;

        // Déjà dans une transaction : une vente qui enregistre son règlement,
        // par exemple. C'est la transaction englobante qui décide.
        if (bdd.CurrentTransaction is not null)
        {
            return await operation();
        }

        // La stratégie de reprise de PostgreSQL exige que la transaction soit
        // rejouée en entier en cas de coupure passagère.
        return await bdd.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var transaction = await bdd.BeginTransactionAsync(cancellationToken);

            try
            {
                var resultat = await operation();
                await transaction.CommitAsync(cancellationToken);
                return resultat;
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                (contexte as DbContext)?.ChangeTracker.Clear();
                throw;
            }
        });
    }

    public static Task EnTransactionAsync(
        this IApplicationDbContext contexte, Func<Task> operation, CancellationToken cancellationToken = default)
        => contexte.EnTransactionAsync(async () =>
        {
            await operation();
            return true;
        }, cancellationToken);
}
