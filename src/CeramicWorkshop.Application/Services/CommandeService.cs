using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.CustomOrders;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Commandes personnalisées : pièces sur mesure demandées par un client.
/// Une date limite est obligatoire (règle n°16), les retards sont signalés
/// (règle n°17) et les photos comme les notes sont conservées (règle n°19).
/// </summary>
public class CommandeService : ICommandeService
{
    /// <summary>Enchaînement des étapes d'une commande personnalisée.</summary>
    public static readonly IReadOnlyList<CustomOrderStatus> Etapes = new[]
    {
        CustomOrderStatus.Commande,
        CustomOrderStatus.Conception,
        CustomOrderStatus.ValidationClient,
        CustomOrderStatus.Production,
        CustomOrderStatus.Cuisson,
        CustomOrderStatus.Decoration,
        CustomOrderStatus.ControleQualite,
        CustomOrderStatus.Pret,
        CustomOrderStatus.Livre
    };

    /// <summary>Une commande à moins de trois jours de son échéance est signalée.</summary>
    public const int JoursAlerteEcheance = 3;

    private readonly IApplicationDbContext _context;
    private readonly IReferenceNumberService _numerotation;
    private readonly ICurrentUserService _utilisateurCourant;
    private readonly IDateTimeService _horloge;
    private readonly IAuditService _audit;

    public CommandeService(
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

    public async Task<PagedResult<CommandeDto>> ListerAsync(
        FiltreCommandesRequete requete, CancellationToken cancellationToken = default)
    {
        var requeteBase = ChargerAvecDetails().AsNoTracking();
        var maintenant = _horloge.UtcNow;

        if (requete.Statut is not null)
        {
            requeteBase = requeteBase.Where(c => c.Status == requete.Statut);
        }

        if (requete.ClientId is not null)
        {
            requeteBase = requeteBase.Where(c => c.CustomerId == requete.ClientId);
        }

        if (requete.SeulementEnCours)
        {
            requeteBase = requeteBase.Where(c =>
                c.Status != CustomOrderStatus.Livre && c.Status != CustomOrderStatus.Annule);
        }

        if (requete.SeulementEnRetard)
        {
            requeteBase = requeteBase.Where(c => c.Deadline < maintenant
                                                 && c.Status != CustomOrderStatus.Livre
                                                 && c.Status != CustomOrderStatus.Annule);
        }

        if (requete.SeulementProchesEcheance)
        {
            var limite = maintenant.AddDays(JoursAlerteEcheance);
            requeteBase = requeteBase.Where(c => c.Deadline >= maintenant && c.Deadline <= limite
                                                 && c.Status != CustomOrderStatus.Livre
                                                 && c.Status != CustomOrderStatus.Annule);
        }

        if (!string.IsNullOrWhiteSpace(requete.Recherche))
        {
            var recherche = requete.Recherche.Trim().ToLower();
            requeteBase = requeteBase.Where(c =>
                c.OrderNumber.ToLower().Contains(recherche) ||
                c.Title.ToLower().Contains(recherche) ||
                c.Customer.FullName.ToLower().Contains(recherche));
        }

        var total = await requeteBase.CountAsync(cancellationToken);

        var commandes = await requeteBase
            .OrderBy(c => c.Deadline).ThenByDescending(c => c.Id)
            .Skip((requete.Page - 1) * requete.TaillePage)
            .Take(requete.TaillePage)
            .ToListAsync(cancellationToken);

        return new PagedResult<CommandeDto>(
            commandes.Select(Convertir).ToList(), total, requete.Page, requete.TaillePage);
    }

    public async Task<CommandeDto> ObtenirAsync(int id, CancellationToken cancellationToken = default)
    {
        var commande = await ChargerAvecDetails().AsNoTracking()
                           .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Commande personnalisée", id);

        return Convertir(commande);
    }

    public async Task<CommandeDto> CreerAsync(
        CommandeRequete requete, CancellationToken cancellationToken = default)
    {
        await VerifierAsync(requete, cancellationToken);

        var commande = new CustomOrder
        {
            OrderNumber = await _numerotation.GenererAsync(TypeDocument.Commande, cancellationToken),
            CustomerId = requete.ClientId,
            Title = requete.Titre.Trim(),
            Description = Nettoyer(requete.Description),
            Width = requete.Largeur,
            Height = requete.Hauteur,
            Depth = requete.Profondeur,
            Colors = Nettoyer(requete.Couleurs),
            Materials = Nettoyer(requete.Materiaux),
            Quantity = requete.Quantite,
            UnitPrice = requete.PrixUnitaire,
            DiscountAmount = requete.Remise,
            OrderDate = _horloge.UtcNow,
            Deadline = requete.DateLimite!.Value,
            Status = CustomOrderStatus.Commande,
            AssignedUserId = requete.EmployeId,
            Notes = Nettoyer(requete.Notes)
        };

        commande.TotalAmount = Math.Round(requete.Quantite * requete.PrixUnitaire - requete.Remise, 2);

        _context.CustomOrders.Add(commande);
        await _context.SaveChangesAsync(cancellationToken);

        await _audit.EnregistrerAsync(AuditAction.Creation, nameof(CustomOrder), commande.Id.ToString(),
            $"Création de la commande {commande.OrderNumber} " +
            $"({MontantFormatter.Formater(commande.TotalAmount)}).", null, cancellationToken);

        return await ObtenirAsync(commande.Id, cancellationToken);
    }

    public async Task<CommandeDto> ModifierAsync(
        int id, CommandeRequete requete, CancellationToken cancellationToken = default)
    {
        var commande = await ChargerAvecDetails().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Commande personnalisée", id);

        if (commande.Status is CustomOrderStatus.Livre or CustomOrderStatus.Annule)
        {
            throw new BusinessRuleException(
                $"La commande {commande.OrderNumber} est « {commande.Status.Libelle()} » : " +
                "elle ne peut plus être modifiée.");
        }

        await VerifierAsync(requete, cancellationToken);

        var nouveauTotal = Math.Round(requete.Quantite * requete.PrixUnitaire - requete.Remise, 2);

        if (nouveauTotal < commande.PaidAmount)
        {
            throw new BusinessRuleException(
                $"Le nouveau total ({MontantFormatter.Formater(nouveauTotal)}) est inférieur au montant " +
                $"déjà encaissé ({MontantFormatter.Formater(commande.PaidAmount)}).");
        }

        commande.Title = requete.Titre.Trim();
        commande.Description = Nettoyer(requete.Description);
        commande.Width = requete.Largeur;
        commande.Height = requete.Hauteur;
        commande.Depth = requete.Profondeur;
        commande.Colors = Nettoyer(requete.Couleurs);
        commande.Materials = Nettoyer(requete.Materiaux);
        commande.Quantity = requete.Quantite;
        commande.UnitPrice = requete.PrixUnitaire;
        commande.DiscountAmount = requete.Remise;
        commande.TotalAmount = nouveauTotal;
        commande.Deadline = requete.DateLimite!.Value;
        commande.AssignedUserId = requete.EmployeId;
        commande.Notes = Nettoyer(requete.Notes);

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(CustomOrder), id.ToString(),
            $"Modification de la commande {commande.OrderNumber}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<CommandeDto> ChangerStatutAsync(
        int id, CustomOrderStatus statut, CancellationToken cancellationToken = default)
    {
        var commande = await ChargerAvecDetails().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Commande personnalisée", id);

        if (commande.Status == CustomOrderStatus.Annule)
        {
            throw new BusinessRuleException($"La commande {commande.OrderNumber} est annulée.");
        }

        var positionActuelle = Etapes.ToList().IndexOf(commande.Status);
        var positionCible = Etapes.ToList().IndexOf(statut);

        if (positionCible <= positionActuelle)
        {
            throw new BusinessRuleException(
                $"La commande est déjà à l'étape « {commande.Status.Libelle()} » : " +
                "elle ne peut avancer que vers une étape suivante.");
        }

        // Le solde doit être réglé avant de remettre la pièce au client.
        if (statut == CustomOrderStatus.Livre && commande.RemainingAmount > 0)
        {
            throw new BusinessRuleException(
                $"Il reste {MontantFormatter.Formater(commande.RemainingAmount)} à encaisser " +
                "avant de livrer cette commande.");
        }

        commande.Status = statut;

        if (statut == CustomOrderStatus.Livre)
        {
            commande.DeliveredAt = _horloge.UtcNow;
        }

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Modification, nameof(CustomOrder), id.ToString(),
            $"Commande {commande.OrderNumber} : {statut.Libelle()}.", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<CommandeDto> AjouterPhotoAsync(
        int id, PhotoCommandeRequete requete, CancellationToken cancellationToken = default)
    {
        if (!await _context.CustomOrders.AnyAsync(c => c.Id == id, cancellationToken))
        {
            throw NotFoundException.Pour("Commande personnalisée", id);
        }

        if (string.IsNullOrWhiteSpace(requete.Chemin))
        {
            throw new BusinessRuleException("Sélectionnez une photo à ajouter.");
        }

        var ordre = await _context.CustomOrderImages
            .Where(i => i.CustomOrderId == id)
            .Select(i => (int?)i.SortOrder).MaxAsync(cancellationToken) ?? 0;

        _context.CustomOrderImages.Add(new CustomOrderImage
        {
            CustomOrderId = id,
            FilePath = requete.Chemin.Trim(),
            Caption = Nettoyer(requete.Legende),
            Kind = requete.Type,
            SortOrder = ordre + 1
        });

        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<CommandeDto> AjouterNoteAsync(
        int id, NoteRequete requete, CancellationToken cancellationToken = default)
    {
        if (!await _context.CustomOrders.AnyAsync(c => c.Id == id, cancellationToken))
        {
            throw NotFoundException.Pour("Commande personnalisée", id);
        }

        if (string.IsNullOrWhiteSpace(requete.Contenu))
        {
            throw new BusinessRuleException("La note ne peut pas être vide.");
        }

        _context.CustomOrderNotes.Add(new CustomOrderNote
        {
            CustomOrderId = id,
            Content = requete.Contenu.Trim(),
            UserId = _utilisateurCourant.UserId
        });

        await _context.SaveChangesAsync(cancellationToken);
        return await ObtenirAsync(id, cancellationToken);
    }

    public async Task<CommandeDto> AnnulerAsync(
        int id, string motif, CancellationToken cancellationToken = default)
    {
        var commande = await ChargerAvecDetails().FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Commande personnalisée", id);

        if (commande.Status == CustomOrderStatus.Annule)
        {
            throw new BusinessRuleException($"La commande {commande.OrderNumber} est déjà annulée.");
        }

        if (commande.Status == CustomOrderStatus.Livre)
        {
            throw new BusinessRuleException(
                $"La commande {commande.OrderNumber} est livrée : elle ne peut plus être annulée.");
        }

        if (string.IsNullOrWhiteSpace(motif))
        {
            throw new BusinessRuleException("Indiquez le motif de l'annulation.");
        }

        if (commande.PaidAmount > 0)
        {
            throw new BusinessRuleException(
                $"Un acompte de {MontantFormatter.Formater(commande.PaidAmount)} a été encaissé. " +
                "Annulez d'abord les paiements pour pouvoir annuler la commande.");
        }

        commande.Status = CustomOrderStatus.Annule;
        commande.Notes = string.IsNullOrWhiteSpace(commande.Notes)
            ? $"Annulée : {motif.Trim()}"
            : $"{commande.Notes}\nAnnulée : {motif.Trim()}";

        await _context.SaveChangesAsync(cancellationToken);
        await _audit.EnregistrerAsync(AuditAction.Annulation, nameof(CustomOrder), id.ToString(),
            $"Annulation de la commande {commande.OrderNumber} : {motif.Trim()}", null, cancellationToken);

        return await ObtenirAsync(id, cancellationToken);
    }

    private IQueryable<CustomOrder> ChargerAvecDetails()
        => _context.CustomOrders
            .Include(c => c.Customer)
            .Include(c => c.AssignedUser)
            .Include(c => c.Images)
            .Include(c => c.OrderNotes).ThenInclude(n => n.User);

    private async Task VerifierAsync(CommandeRequete requete, CancellationToken cancellationToken)
    {
        if (!await _context.Customers.AnyAsync(c => c.Id == requete.ClientId, cancellationToken))
        {
            throw new BusinessRuleException("Le client sélectionné n'existe pas.");
        }

        if (string.IsNullOrWhiteSpace(requete.Titre))
        {
            throw new BusinessRuleException("Décrivez brièvement la pièce demandée.");
        }

        if (requete.Quantite <= 0)
        {
            throw new BusinessRuleException("La quantité doit être supérieure à zéro.");
        }

        if (requete.PrixUnitaire < 0 || requete.Remise < 0)
        {
            throw new BusinessRuleException("Le prix et la remise ne peuvent pas être négatifs.");
        }

        // Règle métier n°16 : une commande personnalisée a toujours une date limite.
        if (requete.DateLimite is null)
        {
            throw new BusinessRuleException("La date limite de la commande est obligatoire.");
        }
    }

    private static string? Nettoyer(string? valeur)
        => string.IsNullOrWhiteSpace(valeur) ? null : valeur.Trim();

    private CommandeDto Convertir(CustomOrder c)
    {
        var maintenant = _horloge.UtcNow;
        var active = c.Status != CustomOrderStatus.Livre && c.Status != CustomOrderStatus.Annule;

        return new CommandeDto(
            c.Id,
            c.OrderNumber,
            c.CustomerId,
            c.Customer.FullName,
            c.Customer.PhoneNumber,
            c.Title,
            c.Description,
            c.Width,
            c.Height,
            c.Depth,
            c.Colors,
            c.Materials,
            c.Quantity,
            c.UnitPrice,
            c.DiscountAmount,
            c.TotalAmount,
            c.PaidAmount,
            c.RemainingAmount,
            c.OrderDate,
            c.Deadline,
            c.DeliveredAt,
            c.Status,
            c.Status.Libelle(),
            c.AssignedUserId,
            c.AssignedUser?.FullName,
            c.Notes,
            active && c.Deadline < maintenant,
            (int)Math.Ceiling((c.Deadline - maintenant).TotalDays),
            c.Images.OrderBy(i => i.SortOrder)
                .Select(i => new PhotoCommandeDto(i.Id, i.FilePath, i.Caption, i.Kind, i.Kind.Libelle()))
                .ToList(),
            c.OrderNotes.OrderByDescending(n => n.CreatedAt)
                .Select(n => new NoteCommandeDto(n.Id, n.Content, n.User?.FullName, n.CreatedAt))
                .ToList());
    }
}
