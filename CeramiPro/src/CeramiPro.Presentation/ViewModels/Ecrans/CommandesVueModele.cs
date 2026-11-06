using CeramiPro.Application.Common;
using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Domain.Enums;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels.Formulaires;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels.Ecrans;

/// <summary>
/// Commandes personnalisées : pièces sur mesure demandées à l'atelier, sur
/// Instagram ou WhatsApp. On encaisse l'acompte, on suit l'avancement en
/// quatre étapes, on remet la pièce une fois le solde réglé.
/// </summary>
public partial class CommandesVueModele : ListeVueModele<CommandeDto>
{
    /// <summary>Valeurs particulières du filtre des étapes.</summary>
    private const int EnCours = -1, Toutes = -2;

    /// <summary>Valeur « tous les canaux ».</summary>
    private const int TousCanaux = -1;

    private readonly ICommandeService _service;
    private readonly IDocumentService _documents;
    private readonly FabriqueGaleries _galeries;

    public CommandesVueModele(
        ICommandeService service, IDocumentService documents, FabriqueGaleries galeries, OutilsListe outils)
        : base(outils)
    {
        _service = service;
        _documents = documents;
        _galeries = galeries;

        Filtres = new FiltreListe[]
        {
            new FiltreChoix("Étape",
                new[] { new OptionChamp(EnCours, "En cours (ni livrées, ni annulées)"), new OptionChamp(Toutes, "Toutes") }
                    .Concat(OptionChamp.De<CustomOrderStatus>())
                    .ToList(),
                FiltreChangeAsync, EnCours),
            new FiltreChoix("Reçue par",
                new[] { new OptionChamp(TousCanaux, "Tous les canaux") }
                    .Concat(OptionChamp.De<CanalCommande>())
                    .ToList(),
                FiltreChangeAsync, TousCanaux)
        };
    }

    protected override Type TypeFormulaire => typeof(CommandeFormulaireVueModele);

    public override IReadOnlyList<FiltreListe> Filtres { get; }

    public override IReadOnlyList<ActionListe> Actions => new ActionListe[]
    {
        new("💰  Encaisser un acompte", EncaisserCommand,
            Aide: "Acompte ou solde versé par le client : le reste à payer se met à jour."),
        new("Étape suivante", AvancerCommand,
            Aide: "Reçue → En fabrication → Prête → Livrée."),
        new("Marquer comme livrée", LivrerCommand,
            Aide: "La pièce est remise au client. Le solde doit avoir été encaissé."),
        new("🧾  Reçu de la commande", ImprimerRecuCommand,
            Aide: "Récapitulatif de la commande, des acomptes versés et du reste à payer."),
        new("📷  Photos", PhotosCommand,
            Aide: "Modèle envoyé par le client, croquis, photos de la pièce."),
        new("👤  Nouveau client", NouveauClientCommand,
            Aide: "Créer la fiche d'un nouveau client avant d'enregistrer sa commande."),
        new("Annuler la commande", AnnulerCommand, Destructive: true,
            Aide: "Un acompte déjà versé est alors remboursé au client.")
    };

    [RelayCommand]
    private Task EncaisserAsync()
    {
        if (ElementSelectionne is not { } commande)
        {
            Outils?.Dialogue.Avertissement("Choisissez d'abord une commande dans le tableau.");
            return Task.CompletedTask;
        }

        if (commande.Statut == CustomOrderStatus.Annulee || commande.Reste <= 0)
        {
            Outils?.Dialogue.Information($"La commande {commande.Numero} est soldée : il n'y a rien à encaisser.");
            return Task.CompletedTask;
        }

        return OuvrirFormulaireAsync<PaiementFormulaireVueModele>(formulaire =>
        {
            formulaire.Preselectionner(ReglementRequete.CodeCommande(commande.Id), commande.Reste);
            return Task.CompletedTask;
        });
    }

    [RelayCommand]
    private Task AvancerAsync() => AgirAsync(
        async commande =>
        {
            if (EtapeApres(commande.Statut) is not { } suivante)
            {
                throw new RegleMetierException(
                    $"La commande {commande.Numero} est « {commande.StatutLibelle} » : "
                    + "elle n'a plus d'étape suivante.");
            }

            await _service.ChangerStatutAsync(commande.Id, suivante);
        },
        succes: "La commande est passée à l'étape suivante.");

    [RelayCommand]
    private Task LivrerAsync() => AgirAsync(
        commande => _service.ChangerStatutAsync(commande.Id, CustomOrderStatus.Livree),
        confirmation: "La pièce a-t-elle été remise au client ?",
        succes: "La commande est livrée.");

    [RelayCommand]
    private Task ImprimerRecuAsync() => ImprimerAsync(
        commande => _documents.RecuCommandePdfAsync(commande.Id),
        commande => $"recu-{commande.Numero}.pdf");

    [RelayCommand]
    private Task PhotosAsync() => OuvrirFenetreAsync(_galeries.PourCommande);

    [RelayCommand]
    private Task NouveauClientAsync()
        => OuvrirFormulaireAsync<ClientFormulaireVueModele>(_ => Task.CompletedTask);

    [RelayCommand]
    private Task AnnulerAsync()
    {
        var commande = ElementSelectionne;
        var acompte = commande is { Paye: > 0 };

        return AgirAvecMotifAsync(
            (choisie, motif) => _service.AnnulerAsync(choisie.Id, motif, rembourser: acompte),
            question: acompte
                ? $"Le client a déjà versé {Formatage.Montant(commande!.Paye)}. Annuler la commande revient à " +
                  "lui rembourser cette somme : ses paiements seront annulés.\n\nPourquoi l'annuler ?"
                : "Pourquoi annuler cette commande ? Le motif reste dans l'historique.",
            succes: acompte
                ? "La commande est annulée : rendez l'acompte au client."
                : "La commande est annulée.");
    }

    /// <summary>
    /// Étape qui suit dans le parcours d'une commande. L'annulation n'en
    /// fait pas partie : elle a son propre bouton.
    /// </summary>
    private static CustomOrderStatus? EtapeApres(CustomOrderStatus etape)
        => Application.Services.CommandeService.Etapes
            .SkipWhile(e => e != etape)
            .Skip(1)
            .Cast<CustomOrderStatus?>()
            .FirstOrDefault();

    public override string Titre => "Commandes";

    public override string Introduction =>
        "Pièces sur mesure demandées par les clients, à l'atelier ou sur Instagram et WhatsApp. " +
        "Les commandes en retard sont signalées.";

    protected override Task<PagedResult<CommandeDto>> LireAsync()
    {
        var etape = ((FiltreChoix)Filtres[0]).Valeur;
        var canal = ((FiltreChoix)Filtres[1]).Valeur;

        return _service.ListerAsync(new FiltreCommandesRequete
        {
            Page = Page,
            TaillePage = TaillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            SeulementEnCours = etape == EnCours,
            Statut = etape >= 0 ? (CustomOrderStatus)etape : null,
            Canal = canal >= 0 ? (CanalCommande)canal : null
        });
    }

    /// <summary>Colonnes du tableau, dans l'ordre d'affichage.</summary>
    public override IReadOnlyList<ColonneListe> Colonnes { get; } = new ColonneListe[]
    {
        new("Numéro", "Numero", ColonneAlignement.Gauche),
        new("Client", "ClientNom", ColonneAlignement.Gauche),
        new("Commande", "Titre", ColonneAlignement.Gauche),
        new("Reçue par", "CanalLibelle", ColonneAlignement.Gauche),
        new("À livrer le", "DateLimite", ColonneAlignement.Gauche, FormatColonne.Date),
        new("Total", "Total", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Versé", "Paye", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Reste", "Reste", ColonneAlignement.Droite, FormatColonne.Montant),
        new("Étape", "StatutLibelle", ColonneAlignement.Gauche),
        new("En retard", "EnRetard", ColonneAlignement.Centre)
    };
}
