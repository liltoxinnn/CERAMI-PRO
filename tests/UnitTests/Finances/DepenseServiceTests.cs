using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Finances;
using CeramicWorkshop.UnitTests.Aides;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace CeramicWorkshop.UnitTests.Finances;

public class DepenseServiceTests : IDisposable
{
    private readonly AtelierDeTest _atelier = new();

    [Fact]
    public async Task Une_depense_recoit_une_reference_numerotee()
    {
        var depense = await _atelier.Depenses.CreerAsync(new DepenseRequete
        {
            CategorieId = _atelier.CategorieDepenseId,
            Montant = 12500m,
            Description = "Facture d'électricité de février"
        });

        depense.Reference.Should().StartWith("DEP-");
        depense.Montant.Should().Be(12500m);
        depense.CategorieNom.Should().Be("Électricité");
        depense.Utilisateur.Should().NotBeNull();
    }

    [Fact]
    public async Task Une_depense_sans_montant_est_refusee()
    {
        var action = async () => await _atelier.Depenses.CreerAsync(new DepenseRequete
        {
            CategorieId = _atelier.CategorieDepenseId,
            Montant = 0m,
            Description = "Facture d'électricité"
        });

        await action.Should().ThrowAsync<BusinessRuleException>();
    }

    [Fact]
    public async Task Une_depense_sur_une_categorie_inconnue_est_refusee()
    {
        var action = async () => await _atelier.Depenses.CreerAsync(new DepenseRequete
        {
            CategorieId = 9999,
            Montant = 500m,
            Description = "Transport"
        });

        await action.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task Modifier_une_depense_met_a_jour_le_montant()
    {
        var id = await _atelier.CreerDepenseAsync(5000m);

        var modifiee = await _atelier.Depenses.ModifierAsync(id, new DepenseRequete
        {
            CategorieId = _atelier.CategorieDepenseId,
            Montant = 7300m,
            Description = "Facture d'électricité corrigée"
        });

        modifiee.Montant.Should().Be(7300m);
        modifiee.Description.Should().Be("Facture d'électricité corrigée");
    }

    [Fact]
    public async Task Supprimer_une_depense_conserve_la_trace_regle_15()
    {
        var id = await _atelier.CreerDepenseAsync(5000m);

        await _atelier.Depenses.SupprimerAsync(id, "Doublon de saisie");

        var conservee = await _atelier.Contexte.Expenses
            .IgnoreQueryFilters().AsNoTracking().FirstAsync(d => d.Id == id);

        conservee.IsDeleted.Should().BeTrue();
        conservee.Description.Should().Contain("Doublon de saisie");

        var listage = await _atelier.Depenses.ListerAsync(new FiltreDepensesRequete());
        listage.Elements.Should().NotContain(d => d.Id == id);
    }

    [Fact]
    public async Task Supprimer_sans_motif_est_refuse()
    {
        var id = await _atelier.CreerDepenseAsync(5000m);

        var action = async () => await _atelier.Depenses.SupprimerAsync(id, "   ");

        await action.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("*motif*");
    }

    [Fact]
    public async Task Le_total_ne_retient_que_les_depenses_de_la_periode()
    {
        var aujourdhui = _atelier.Horloge.UtcNow;

        await _atelier.CreerDepenseAsync(3000m, date: aujourdhui);
        await _atelier.CreerDepenseAsync(2000m, date: aujourdhui.AddDays(-2));
        await _atelier.CreerDepenseAsync(9999m, date: aujourdhui.AddDays(-40));

        var total = await _atelier.Depenses.TotalAsync(aujourdhui.AddDays(-7), aujourdhui);

        total.Should().Be(5000m);
    }

    [Fact]
    public async Task Le_listage_filtre_par_categorie_et_par_periode()
    {
        var autre = new CeramicWorkshop.Domain.Entities.Expenses.ExpenseCategory { Name = "Transport" };
        _atelier.Contexte.ExpenseCategories.Add(autre);
        await _atelier.Contexte.SaveChangesAsync();

        await _atelier.CreerDepenseAsync(3000m);
        await _atelier.Depenses.CreerAsync(new DepenseRequete
        {
            CategorieId = autre.Id,
            Montant = 1500m,
            Description = "Livraison de commande"
        });

        var resultat = await _atelier.Depenses.ListerAsync(new FiltreDepensesRequete
        {
            CategorieId = autre.Id
        });

        resultat.Elements.Should().ContainSingle()
            .Which.CategorieNom.Should().Be("Transport");
    }

    public void Dispose() => _atelier.Dispose();
}
