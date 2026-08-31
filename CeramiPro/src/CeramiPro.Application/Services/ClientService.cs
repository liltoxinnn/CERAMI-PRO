using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Entities.Customers;
using CeramiPro.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramiPro.Application.Services;

/// <summary>
/// Clients de l'atelier : coordonnées, historique et montants restant dus.
/// Le solde est toujours recalculé à partir des ventes et des commandes.
/// </summary>
public class ClientService : IClientService
{
    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly IUtilisateurCourant _utilisateurCourant;
    private readonly IAuditService _audit;

    public ClientService(
        IApplicationDbContext context,
        IReferenceNumberService numerotation,
        IUtilisateurCourant utilisateurCourant,
        IAuditService audit)
    {
        _context = context;
        _numerotation = numerotation;
        _utilisateurCourant = utilisateurCourant;
        _audit = audit;
    }

    public async Task<PagedResult<ClientDto>> ListerAsync(
        FiltreClientsRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = _context.Customers.AsNoTracking().AsQueryable();

        if (!requete.InclureInactifs)
        {
            requeteBase = requeteBase.Where(c => c.IsActive);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(c =>
                c.FullName.ToLower().Contains(recherche) ||
                c.CustomerNumber.ToLower().Contains(recherche) ||
                (c.PhoneNumber != null && c.PhoneNumber.Contains(recherche)) ||
                (c.Email != null && c.Email.ToLower().Contains(recherche)));
        }

        var elements = await requeteBase.Select(Projeter()).ToListAsync(cancellationToken);

        if (requete.SeulementAvecDette)
        {
            elements = elements.Where(c => c.Reste > 0).ToList();
        }

        var total = elements.Count;

        var page = elements
            .OrderBy(c => c.Nom)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToList();

        return new PagedResult<ClientDto>(page, total, requete.Page, requete.TaillePage);
    }

    public async Task<ClientDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
        => await _context.Customers.AsNoTracking().Where(c => c.Id == id)
               .Select(Projeter()).FirstOrDefaultAsync(cancellationToken)
           ?? throw IntrouvableException.Pour("Client", id);

    public async Task<ClientDto> CreerAsync(
        ClientRequete requete, CancellationToken cancellationToken = default)
    {
        var client = new Customer
        {
            CustomerNumber = await _numerotation.GenererAsync(TypeDocument.Client, cancellationToken),
            FullName = requete.Nom.Trim(),
            PhoneNumber = Nettoyer(requete.Telephone),
            Email = Nettoyer(requete.Email),
            Address = Nettoyer(requete.Adresse),
            City = Nettoyer(requete.Ville),
            Notes = Nettoyer(requete.Notes),
            IsActive = requete.Actif
        };

        _context.Customers.Add(client);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(Customer), client.Id.ToString(),
            $"Création du client « {client.FullName} ».", null, cancellationToken);

        return await ObtenirAsync(client.Id, cancellationToken);
    }

    public async Task<ClientDto> ModifierAsync(
        int id, ClientRequete requete, CancellationToken cancellationToken = default)
    {
        var client = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                     ?? throw IntrouvableException.Pour("Client", id);

        client.FullName = requete.Nom.Trim();
        client.PhoneNumber = Nettoyer(requete.Telephone);
        client.Email = Nettoyer(requete.Email);
        client.Address = Nettoyer(requete.Adresse);
        client.City = Nettoyer(requete.Ville);
        client.Notes = Nettoyer(requete.Notes);
        client.IsActive = requete.Actif;

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(Customer), id.ToString(),
            $"Modification du client « {client.FullName} ».", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task SupprimerAsync(int id, CancellationToken cancellationToken = default)
    {
        var client = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                     ?? throw IntrouvableException.Pour("Client", id);

        var utilise = await _context.Sales.IgnoreQueryFilters().AnyAsync(v => v.CustomerId == id, cancellationToken)
                      || await _context.CustomOrders.IgnoreQueryFilters()
                          .AnyAsync(c => c.CustomerId == id, cancellationToken);

        if (utilise)
        {
            throw new RegleMetierException(
                $"Le client « {client.FullName} » possède un historique. Désactivez-le au lieu de le supprimer.");
        }

        _context.Customers.Remove(client);
        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Suppression, nameof(Customer), id.ToString(),
            $"Suppression du client « {client.FullName} ».", null, cancellationToken);
    }

    public async Task<IReadOnlyList<NoteClientDto>> ListerNotesAsync(
        int id, CancellationToken cancellationToken = default)
        => await _context.CustomerNotes
            .Include(n => n.User)
            .AsNoTracking()
            .Where(n => n.CustomerId == id)
            .OrderByDescending(n => n.CreatedAt)
            .Select(n => new NoteClientDto(n.Id, n.Content, n.User != null ? n.User.FullName : null, n.CreatedAt))
            .ToListAsync(cancellationToken);

    public async Task<NoteClientDto> AjouterNoteAsync(
        int id, NoteRequete requete, CancellationToken cancellationToken = default)
    {
        if (!await _context.Customers.AnyAsync(c => c.Id == id, cancellationToken))
        {
            throw IntrouvableException.Pour("Client", id);
        }

        if (string.IsNullOrWhiteSpace(requete.Contenu))
        {
            throw new RegleMetierException("La note ne peut pas être vide.");
        }

        var note = new CustomerNote
        {
            CustomerId = id,
            Content = requete.Contenu.Trim(),
            UserId = _utilisateurCourant.UtilisateurId
        };

        _context.CustomerNotes.Add(note);
        await _context.SaveChangesAsync(cancellationToken);

        return (await ListerNotesAsync(id, cancellationToken)).First(n => n.Id == note.Id);
    }

    public async Task<IReadOnlyList<DetteClientDto>> ListerDettesAsync(
        CancellationToken cancellationToken = default)
    {
        var clients = await _context.Customers.AsNoTracking().Select(Projeter()).ToListAsync(cancellationToken);

        var echeances = await _context.CustomOrders
            .AsNoTracking()
            .Where(c => c.Status != CustomOrderStatus.Annule)
            .GroupBy(c => c.CustomerId)
            .Select(g => new { ClientId = g.Key, Echeance = g.Min(c => (DateTime?)c.Deadline) })
            .ToListAsync(cancellationToken);

        return clients
            .Where(c => c.Reste > 0)
            .Select(c => new DetteClientDto(
                c.Id, c.Nom, c.Telephone, c.TotalDepense, c.TotalPaye, c.Reste,
                echeances.FirstOrDefault(e => e.ClientId == c.Id)?.Echeance))
            .OrderByDescending(c => c.Reste)
            .ToList();
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    /// <summary>
    /// Le total dépensé additionne les ventes confirmées et les commandes non annulées ;
    /// le total payé provient des paiements enregistrés.
    /// </summary>
    private static System.Linq.Expressions.Expression<Func<Customer, ClientDto>> Projeter()
        => c => new ClientDto(
            c.Id,
            c.CustomerNumber,
            c.FullName,
            c.PhoneNumber,
            c.Email,
            c.Address,
            c.City,
            c.Notes,
            c.IsActive,
            (c.Sales.Where(v => v.Status == SaleStatus.Confirmee).Sum(v => (decimal?)v.TotalAmount) ?? 0m)
            + (c.CustomOrders.Where(o => o.Status != CustomOrderStatus.Annule)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m),
            c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m,
            (c.Sales.Where(v => v.Status == SaleStatus.Confirmee).Sum(v => (decimal?)v.TotalAmount) ?? 0m)
            + (c.CustomOrders.Where(o => o.Status != CustomOrderStatus.Annule)
                .Sum(o => (decimal?)o.TotalAmount) ?? 0m)
            - (c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m),
            c.Sales.Count(v => v.Status == SaleStatus.Confirmee),
            c.CustomOrders.Count(o => o.Status != CustomOrderStatus.Annule),
            c.Sales.Max(v => (DateTime?)v.SaleDate));
}
