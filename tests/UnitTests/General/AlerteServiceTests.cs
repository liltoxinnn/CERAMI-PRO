using CeramicWorkshop.Application.DTOs.Alertes;
using CeramicWorkshop.Application.DTOs.Commercial;
using CeramicWorkshop.Domain.Enums;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.General;

public class AlerteServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    public AlerteServiceTests()
    {
        _atelier.AccorderTousLesDroits();
        _atelier.PreparerAlertes();
    }

    [Fact]
    public async Task Un_atelier_a_jour_n_affiche_aucune_alerte()
    {
        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().BeEmpty();
    }

    [Fact]
    public async Task Un_produit_sous_son_seuil_declenche_une_alerte()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 5m);

        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().ContainSingle()
            .Which.Type.Should().Be(NotificationType.StockFaible);
        alertes[0].Gravite.Should().Be(NotificationSeverity.Avertissement);
        alertes[0].Message.Should().Contain("1");
    }

    [Fact]
    public async Task Un_produit_epuise_declenche_une_alerte_critique()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 0m, stockMinimum: 5m);

        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().ContainSingle()
            .Which.Gravite.Should().Be(NotificationSeverity.Critique);
    }

    [Fact]
    public async Task Une_matiere_sous_son_seuil_declenche_une_alerte()
    {
        await _atelier.CreerMatiereAsync("Argile", stockInitial: 2m, stockMinimum: 10m);

        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().Contain(a => a.Type == NotificationType.MatiereInsuffisante);
    }

    [Fact]
    public async Task Une_alerte_disparait_quand_le_stock_est_reapprovisionne()
    {
        var produitId = await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 5m);

        (await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete())).Should().HaveCount(1);

        await _atelier.Inventaire.EnregistrerAsync(new Application.DTOs.Stock.MouvementStockRequete
        {
            TypeArticle = InventoryItemType.ProduitFini,
            TypeMouvement = InventoryTransactionType.Ajustement,
            ProduitId = produitId,
            Quantite = 20m,
            Notes = "Réapprovisionnement"
        });

        await _atelier.Contexte.SaveChangesAsync();

        (await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete())).Should().BeEmpty();
    }

    [Fact]
    public async Task Une_alerte_n_est_pas_dupliquee_a_chaque_consultation()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 5m);

        await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());
        await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());
        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().HaveCount(1);
    }

    [Fact]
    public async Task Une_commande_depassee_declenche_une_alerte_critique()
    {
        var clientId = await _atelier.CreerClientAsync();

        await _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId,
            Titre = "Service à thé personnalisé",
            Quantite = 2m,
            PrixUnitaire = 3500m,
            DateLimite = _atelier.Horloge.UtcNow.AddDays(-2)
        });

        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().Contain(a => a.Type == NotificationType.CommandeRetard
                                      && a.Gravite == NotificationSeverity.Critique);
    }

    [Fact]
    public async Task Une_commande_proche_de_l_echeance_est_signalee()
    {
        var clientId = await _atelier.CreerClientAsync();

        await _atelier.Commandes.CreerAsync(new CommandeRequete
        {
            ClientId = clientId,
            Titre = "Vase sur mesure",
            Quantite = 1m,
            PrixUnitaire = 3500m,
            DateLimite = _atelier.Horloge.UtcNow.AddDays(2)
        });

        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());

        alertes.Should().Contain(a => a.Type == NotificationType.CommandeEcheance);
    }

    [Fact]
    public async Task Marquer_une_alerte_comme_lue_la_retire_du_filtre_des_non_lues()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 5m);

        var alertes = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete());
        await _atelier.Alertes.MarquerLueAsync(alertes[0].Id);

        var nonLues = await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete
        {
            SeulementNonLues = true
        });

        nonLues.Should().BeEmpty();
        (await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete())).Should().HaveCount(1);
    }

    [Fact]
    public async Task Le_resume_compte_les_alertes_non_lues_et_critiques()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 0m, stockMinimum: 5m);
        await _atelier.CreerMatiereAsync("Argile", stockInitial: 2m, stockMinimum: 10m);

        var resume = await _atelier.Alertes.ResumeAsync();

        resume.Total.Should().Be(2);
        resume.NonLues.Should().Be(2);
        resume.Critiques.Should().Be(1);
    }

    [Fact]
    public async Task Desactiver_un_reglage_supprime_les_alertes_correspondantes()
    {
        await _atelier.CreerProduitAsync("Vase", stockInitial: 1m, stockMinimum: 5m);

        (await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete())).Should().HaveCount(1);

        var reglages = await _atelier.Alertes.ListerReglagesAsync();
        var stock = reglages.First(r => r.Type == NotificationType.StockFaible);
        stock.Active = false;

        await _atelier.Alertes.ModifierReglageAsync(stock.Id, stock);

        (await _atelier.Alertes.ListerAsync(new FiltreAlertesRequete())).Should().BeEmpty();
    }

    [Fact]
    public async Task Un_delai_d_alerte_hors_limites_est_refuse()
    {
        var reglages = await _atelier.Alertes.ListerReglagesAsync();
        var echeance = reglages.First(r => r.Type == NotificationType.CommandeEcheance);
        echeance.SeuilJours = 400;

        var action = async () => await _atelier.Alertes.ModifierReglageAsync(echeance.Id, echeance);

        await action.Should().ThrowAsync<Application.Common.BusinessRuleException>();
    }

    public void Dispose() => _atelier.Dispose();
}
