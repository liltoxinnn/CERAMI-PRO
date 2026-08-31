using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Alertes;
using CeramicWorkshop.Application.Interfaces;
using CeramicWorkshop.Domain.Common;
using CeramicWorkshop.Domain.Entities.Notifications;
using CeramicWorkshop.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.Application.Services;

/// <summary>
/// Centre d'alertes de l'atelier.
///
/// Les alertes sont recalculées à chaque consultation à partir de l'état réel
/// de l'atelier : rien n'est inventé ni conservé une fois le problème résolu.
/// Une alerte déjà présente n'est pas dupliquée, et celle dont la cause a
/// disparu est retirée automatiquement.
/// </summary>
public class AlerteService : IAlerteService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeService _horloge;

    public AlerteService(IApplicationDbContext context, IDateTimeService horloge)
    {
        _context = context;
        _horloge = horloge;
    }

    public async Task<IReadOnlyList<AlerteDto>> ListerAsync(
        FiltreAlertesRequete requete, CancellationToken cancellationToken = default)
    {
        await RecalculerAsync(cancellationToken);

        var alertes = _context.Notifications.AsNoTracking().AsQueryable();

        if (requete.SeulementNonLues)
        {
            alertes = alertes.Where(a => !a.IsRead);
        }

        if (requete.Gravite is not null)
        {
            alertes = alertes.Where(a => a.Severity == requete.Gravite);
        }

        var liste = await alertes
            .OrderByDescending(a => a.Severity)
            .ThenByDescending(a => a.CreatedAt)
            .Take(200)
            .ToListAsync(cancellationToken);

        return liste.Select(Convertir).ToList();
    }

    public async Task<ResumeAlertesDto> ResumeAsync(CancellationToken cancellationToken = default)
    {
        await RecalculerAsync(cancellationToken);

        var alertes = await _context.Notifications.AsNoTracking()
            .Select(a => new { a.IsRead, a.Severity })
            .ToListAsync(cancellationToken);

        return new ResumeAlertesDto(
            alertes.Count,
            alertes.Count(a => !a.IsRead),
            alertes.Count(a => a.Severity == NotificationSeverity.Critique));
    }

    public async Task MarquerLueAsync(int id, CancellationToken cancellationToken = default)
    {
        var alerte = await _context.Notifications.FirstOrDefaultAsync(a => a.Id == id, cancellationToken)
                     ?? throw NotFoundException.Pour("Alerte", id);

        if (!alerte.IsRead)
        {
            alerte.IsRead = true;
            alerte.ReadAt = _horloge.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task ToutMarquerLuAsync(CancellationToken cancellationToken = default)
    {
        var alertes = await _context.Notifications.Where(a => !a.IsRead).ToListAsync(cancellationToken);

        foreach (var alerte in alertes)
        {
            alerte.IsRead = true;
            alerte.ReadAt = _horloge.UtcNow;
        }

        if (alertes.Count > 0)
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<IReadOnlyList<ReglageAlerteDto>> ListerReglagesAsync(
        CancellationToken cancellationToken = default)
    {
        var reglages = await _context.NotificationSettings.AsNoTracking()
            .OrderBy(r => r.Type)
            .ToListAsync(cancellationToken);

        return reglages.Select(Convertir).ToList();
    }

    public async Task<ReglageAlerteDto> ModifierReglageAsync(
        int id, ReglageAlerteDto reglage, CancellationToken cancellationToken = default)
    {
        var existant = await _context.NotificationSettings
                           .FirstOrDefaultAsync(r => r.Id == id, cancellationToken)
                       ?? throw NotFoundException.Pour("Réglage d'alerte", id);

        if (reglage.SeuilJours is < 0 or > 365)
        {
            throw new BusinessRuleException("Le délai d'alerte doit être compris entre 0 et 365 jours.");
        }

        existant.IsEnabled = reglage.Active;
        existant.ThresholdDays = reglage.SeuilJours;
        existant.ThresholdValue = reglage.SeuilValeur;

        await _context.SaveChangesAsync(cancellationToken);

        return Convertir(existant);
    }

    // --------------------------------------------------------- Recalcul

    private async Task RecalculerAsync(CancellationToken cancellationToken)
    {
        var reglages = await _context.NotificationSettings.AsNoTracking()
            .ToDictionaryAsync(r => r.Type, cancellationToken);

        var maintenant = _horloge.UtcNow;
        var attendues = new List<Notification>();

        attendues.AddRange(await StockFaibleAsync(reglages, cancellationToken));
        attendues.AddRange(await MatieresInsuffisantesAsync(reglages, cancellationToken));
        attendues.AddRange(await CommandesAsync(reglages, maintenant, cancellationToken));
        attendues.AddRange(await ProductionAsync(reglages, maintenant, cancellationToken));
        attendues.AddRange(await DettesAsync(reglages, cancellationToken));

        var existantes = await _context.Notifications.ToListAsync(cancellationToken);
        var modifie = false;

        // Les alertes dont la cause a disparu sont retirées.
        foreach (var obsolete in existantes.Where(e =>
                     e.Type != NotificationType.Information &&
                     !attendues.Any(a => a.Type == e.Type
                                         && a.EntityName == e.EntityName
                                         && a.EntityId == e.EntityId)))
        {
            _context.Notifications.Remove(obsolete);
            modifie = true;
        }

        foreach (var attendue in attendues)
        {
            var deja = existantes.FirstOrDefault(e => e.Type == attendue.Type
                                                     && e.EntityName == attendue.EntityName
                                                     && e.EntityId == attendue.EntityId);

            if (deja is null)
            {
                _context.Notifications.Add(attendue);
                modifie = true;
                continue;
            }

            // Le message est rafraîchi : les quantités et les retards évoluent.
            if (deja.Message != attendue.Message || deja.Severity != attendue.Severity)
            {
                deja.Message = attendue.Message;
                deja.Severity = attendue.Severity;
                modifie = true;
            }
        }

        if (!modifie)
        {
            return;
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException)
        {
            // Deux écrans peuvent recalculer les alertes en même temps. La base
            // refuse alors le doublon : l'autre requête a déjà fait le travail,
            // il n'y a rien à reprendre.
            _context.AnnulerModificationsEnAttente();
        }
    }

    private async Task<List<Notification>> StockFaibleAsync(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages,
        CancellationToken cancellationToken)
    {
        if (!Actif(reglages, NotificationType.StockFaible))
        {
            return new List<Notification>();
        }

        var produits = await _context.Products.AsNoTracking()
            .Where(p => p.IsActive && p.CurrentStock <= p.MinimumStock)
            .Select(p => new { p.Id, p.Name, p.Reference, p.CurrentStock, p.MinimumStock })
            .ToListAsync(cancellationToken);

        return produits.Select(p => Creer(
            NotificationType.StockFaible,
            p.CurrentStock <= 0 ? NotificationSeverity.Critique : NotificationSeverity.Avertissement,
            $"Stock faible : {p.Name}",
            p.CurrentStock <= 0
                ? "Il ne reste aucune pièce en stock."
                : $"Il reste {MontantFormatter.FormaterQuantite(p.CurrentStock, "pièce")} " +
                  $"pour un minimum de {MontantFormatter.FormaterQuantite(p.MinimumStock, "pièce")}.",
            "Product", p.Id,
            $"produits?recherche={Uri.EscapeDataString(p.Reference)}")).ToList();
    }

    private async Task<List<Notification>> MatieresInsuffisantesAsync(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages,
        CancellationToken cancellationToken)
    {
        if (!Actif(reglages, NotificationType.MatiereInsuffisante))
        {
            return new List<Notification>();
        }

        var matieres = await _context.Materials.AsNoTracking()
            .Where(m => m.IsActive && m.CurrentQuantity <= m.MinimumStock)
            .Select(m => new { m.Id, m.Name, m.Reference, m.CurrentQuantity, m.MinimumStock, Unite = m.Unit!.Code })
            .ToListAsync(cancellationToken);

        return matieres.Select(m => Creer(
            NotificationType.MatiereInsuffisante,
            m.CurrentQuantity <= 0 ? NotificationSeverity.Critique : NotificationSeverity.Avertissement,
            $"Matière à réapprovisionner : {m.Name}",
            m.CurrentQuantity <= 0
                ? "Le stock est épuisé."
                : $"Il reste {MontantFormatter.FormaterQuantite(m.CurrentQuantity, m.Unite)} " +
                  $"pour un minimum de {MontantFormatter.FormaterQuantite(m.MinimumStock, m.Unite)}.",
            "Material", m.Id,
            $"matieres?recherche={Uri.EscapeDataString(m.Reference)}")).ToList();
    }

    private async Task<List<Notification>> CommandesAsync(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages,
        DateTime maintenant,
        CancellationToken cancellationToken)
    {
        var alertes = new List<Notification>();

        var commandes = await _context.CustomOrders.AsNoTracking()
            .Where(c => c.Status != CustomOrderStatus.Livre && c.Status != CustomOrderStatus.Annule)
            .Select(c => new
            {
                c.Id, c.OrderNumber, c.Deadline, Client = c.Customer!.FullName
            })
            .ToListAsync(cancellationToken);

        if (Actif(reglages, NotificationType.CommandeRetard))
        {
            alertes.AddRange(commandes.Where(c => c.Deadline < maintenant).Select(c => Creer(
                NotificationType.CommandeRetard,
                NotificationSeverity.Critique,
                $"Commande en retard : {c.OrderNumber}",
                $"La commande de {c.Client} devait être livrée le " +
                $"{MontantFormatter.FormaterDate(c.Deadline)}.",
                "CustomOrder", c.Id,
                $"commandes?recherche={Uri.EscapeDataString(c.OrderNumber)}")));
        }

        if (Actif(reglages, NotificationType.CommandeEcheance))
        {
            var jours = Jours(reglages, NotificationType.CommandeEcheance, 3);
            var limite = maintenant.AddDays(jours);

            alertes.AddRange(commandes
                .Where(c => c.Deadline >= maintenant && c.Deadline <= limite)
                .Select(c => Creer(
                    NotificationType.CommandeEcheance,
                    NotificationSeverity.Avertissement,
                    $"Échéance proche : {c.OrderNumber}",
                    $"La commande de {c.Client} est à livrer le " +
                    $"{MontantFormatter.FormaterDate(c.Deadline)}.",
                    "CustomOrder", c.Id,
                    $"commandes?recherche={Uri.EscapeDataString(c.OrderNumber)}")));
        }

        return alertes;
    }

    private async Task<List<Notification>> ProductionAsync(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages,
        DateTime maintenant,
        CancellationToken cancellationToken)
    {
        var alertes = new List<Notification>();

        var ordres = await _context.ProductionOrders.AsNoTracking()
            .Where(o => o.Status != ProductionStatus.Termine && o.Status != ProductionStatus.Annule)
            .Select(o => new
            {
                o.Id, o.ProductionNumber, o.PlannedEndDate, o.Status, o.UpdatedAt, o.CreatedAt,
                Produit = o.Product!.Name
            })
            .ToListAsync(cancellationToken);

        if (Actif(reglages, NotificationType.ProductionRetard))
        {
            alertes.AddRange(ordres
                .Where(o => o.PlannedEndDate is not null && o.PlannedEndDate < maintenant)
                .Select(o => Creer(
                    NotificationType.ProductionRetard,
                    NotificationSeverity.Critique,
                    $"Production en retard : {o.ProductionNumber}",
                    $"{o.Produit} devait être terminé le " +
                    $"{MontantFormatter.FormaterDate(o.PlannedEndDate!.Value)}.",
                    "ProductionOrder", o.Id,
                    $"production?recherche={Uri.EscapeDataString(o.ProductionNumber)}")));
        }

        if (Actif(reglages, NotificationType.AttenteProlongee))
        {
            var jours = Jours(reglages, NotificationType.AttenteProlongee, 7);
            var limite = maintenant.AddDays(-jours);

            alertes.AddRange(ordres
                .Where(o => (o.UpdatedAt ?? o.CreatedAt) < limite)
                .Select(o => Creer(
                    NotificationType.AttenteProlongee,
                    NotificationSeverity.Avertissement,
                    $"Production sans mouvement : {o.ProductionNumber}",
                    $"{o.Produit} est à l'étape « {o.Status.Libelle()} » depuis plus de {jours} jours.",
                    "ProductionOrder", o.Id,
                    $"production?recherche={Uri.EscapeDataString(o.ProductionNumber)}")));
        }

        return alertes;
    }

    private async Task<List<Notification>> DettesAsync(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages,
        CancellationToken cancellationToken)
    {
        var alertes = new List<Notification>();

        if (Actif(reglages, NotificationType.DetteClient))
        {
            // La dette n'est pas stockée : elle se déduit des ventes et des
            // commandes, moins les règlements déjà encaissés.
            var clients = await _context.Customers.AsNoTracking()
                .Select(c => new
                {
                    c.Id,
                    c.FullName,
                    Reste =
                        (c.Sales.Where(v => v.Status == SaleStatus.Confirmee)
                             .Sum(v => (decimal?)v.TotalAmount) ?? 0m)
                        + (c.CustomOrders.Where(o => o.Status != CustomOrderStatus.Annule)
                            .Sum(o => (decimal?)o.TotalAmount) ?? 0m)
                        - (c.Payments.Sum(p => (decimal?)p.Amount) ?? 0m)
                })
                .ToListAsync(cancellationToken);

            var seuil = reglages[NotificationType.DetteClient].ThresholdValue ?? 0m;

            alertes.AddRange(clients.Where(c => c.Reste > seuil && c.Reste > 0).Select(c => Creer(
                NotificationType.DetteClient,
                NotificationSeverity.Avertissement,
                $"Dette client : {c.FullName}",
                $"{MontantFormatter.Formater(c.Reste)} restent à encaisser.",
                "Customer", c.Id,
                "clients/dettes")));
        }

        if (Actif(reglages, NotificationType.DetteFournisseur))
        {
            var achats = await _context.Purchases.AsNoTracking()
                .Where(a => a.Status != PurchaseStatus.Brouillon
                            && a.Status != PurchaseStatus.Annule
                            && a.TotalAmount > a.PaidAmount)
                .Select(a => new
                {
                    a.Id, a.PurchaseNumber, Fournisseur = a.Supplier!.Name,
                    Reste = a.TotalAmount - a.PaidAmount
                })
                .ToListAsync(cancellationToken);

            var seuil = reglages[NotificationType.DetteFournisseur].ThresholdValue ?? 0m;

            alertes.AddRange(achats.Where(a => a.Reste > seuil).Select(a => Creer(
                NotificationType.DetteFournisseur,
                NotificationSeverity.Information,
                $"Achat à régler : {a.PurchaseNumber}",
                $"{MontantFormatter.Formater(a.Reste)} restent dus à {a.Fournisseur}.",
                "Purchase", a.Id,
                $"achats?recherche={Uri.EscapeDataString(a.PurchaseNumber)}")));
        }

        return alertes;
    }

    // ------------------------------------------------------------ Aides

    private static bool Actif(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages, NotificationType type)
        => reglages.TryGetValue(type, out var reglage) && reglage.IsEnabled;

    private static int Jours(
        IReadOnlyDictionary<NotificationType, NotificationSetting> reglages,
        NotificationType type,
        int defaut)
        => reglages.TryGetValue(type, out var reglage) ? reglage.ThresholdDays ?? defaut : defaut;

    private Notification Creer(
        NotificationType type, NotificationSeverity gravite, string titre, string message,
        string entite, int entiteId, string adresse)
        => new()
        {
            Type = type,
            Severity = gravite,
            Title = titre,
            Message = message,
            EntityName = entite,
            EntityId = entiteId,
            Link = adresse,
            CreatedAt = _horloge.UtcNow
        };

    private static AlerteDto Convertir(Notification alerte)
        => new(alerte.Id, alerte.Type, alerte.Type.Libelle(), alerte.Severity, alerte.Severity.Libelle(),
            alerte.Title, alerte.Message, alerte.Link, alerte.IsRead, alerte.CreatedAt);

    private static ReglageAlerteDto Convertir(NotificationSetting reglage)
        => new()
        {
            Id = reglage.Id,
            Type = reglage.Type,
            TypeLibelle = reglage.Type.Libelle(),
            Explication = Explication(reglage.Type),
            Active = reglage.IsEnabled,
            SeuilJours = reglage.ThresholdDays,
            SeuilValeur = reglage.ThresholdValue,
            AttendDesJours = reglage.Type is NotificationType.CommandeEcheance
                or NotificationType.PaiementEnAttente or NotificationType.ProductionBloquee
                or NotificationType.AttenteProlongee
        };

    private static string Explication(NotificationType type) => type switch
    {
        NotificationType.StockFaible =>
            "Prévient dès qu'un produit fini atteint son stock minimum.",
        NotificationType.MatiereInsuffisante =>
            "Prévient dès qu'une matière première atteint son stock minimum.",
        NotificationType.CommandeEcheance =>
            "Prévient quelques jours avant la date promise au client.",
        NotificationType.CommandeRetard =>
            "Prévient dès qu'une commande dépasse la date promise.",
        NotificationType.PaiementEnAttente =>
            "Prévient lorsqu'un règlement attendu tarde à arriver.",
        NotificationType.DetteClient =>
            "Prévient lorsqu'un client a un reste à payer.",
        NotificationType.DetteFournisseur =>
            "Prévient lorsqu'un achat reste à régler.",
        NotificationType.ProductionBloquee =>
            "Prévient lorsqu'une production ne peut pas avancer.",
        NotificationType.ProductionRetard =>
            "Prévient lorsqu'une production dépasse sa date de fin prévue.",
        NotificationType.AttenteProlongee =>
            "Prévient lorsqu'une production reste trop longtemps à la même étape.",
        _ => "Message d'information."
    };
}
