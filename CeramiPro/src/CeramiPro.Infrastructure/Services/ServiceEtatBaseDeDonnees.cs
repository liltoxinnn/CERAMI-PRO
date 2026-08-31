using CeramiPro.Application.Common;
using CeramiPro.Application.Interfaces;
using CeramiPro.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Interroge réellement PostgreSQL. L'écran affiche ainsi l'état constaté,
/// jamais un message d'attente figé.
/// </summary>
public class ServiceEtatBaseDeDonnees : IServiceEtatBaseDeDonnees
{
    private readonly CeramiProDbContext _contexte;
    private readonly ILogger<ServiceEtatBaseDeDonnees> _journal;

    public ServiceEtatBaseDeDonnees(
        CeramiProDbContext contexte, ILogger<ServiceEtatBaseDeDonnees> journal)
    {
        _contexte = contexte;
        _journal = journal;
    }

    public async Task<EtatBaseDeDonnees> VerifierAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _contexte.Database.CanConnectAsync(cancellationToken)
                ? EtatBaseDeDonnees.Connectee(ParametresAtelier.NomBaseDeDonnees)
                : EtatBaseDeDonnees.Injoignable();
        }
        catch (Exception erreur)
        {
            _journal.LogWarning(erreur, "La base de données n'a pas répondu.");
            return EtatBaseDeDonnees.Injoignable();
        }
    }
}
