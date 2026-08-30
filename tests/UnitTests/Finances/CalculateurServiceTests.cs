using CeramicWorkshop.Application.Common;
using CeramicWorkshop.Application.DTOs.Finances;
using CeramicWorkshop.Application.Services;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.Finances;

public class CalculateurServiceTests
{
    private readonly CalculateurService _calculateurs = new();

    [Fact]
    public void La_surface_multiplie_les_dimensions_puis_ajoute_la_perte()
    {
        var resultat = _calculateurs.Surface(new CalculSurfaceRequete
        {
            Longueur = 10m,
            Largeur = 7.4m,
            NombrePieces = 2,
            PourcentagePerte = 10m
        });

        resultat.SurfaceUnitaire.Should().Be(74m);
        resultat.SurfaceTotale.Should().Be(148m);
        resultat.Perte.Should().Be(14.8m);
        resultat.SurfaceAvecPerte.Should().Be(162.8m);
    }

    [Fact]
    public void Une_surface_sans_perte_reste_identique()
    {
        var resultat = _calculateurs.Surface(new CalculSurfaceRequete
        {
            Longueur = 3m, Largeur = 2m, NombrePieces = 1, PourcentagePerte = 0m
        });

        resultat.Perte.Should().Be(0m);
        resultat.SurfaceAvecPerte.Should().Be(6m);
    }

    [Theory]
    [InlineData(0, 5, 1, 10)]
    [InlineData(5, 0, 1, 10)]
    [InlineData(5, 5, 0, 10)]
    [InlineData(5, 5, 1, 140)]
    public void Une_saisie_incoherente_de_surface_est_refusee(
        decimal longueur, decimal largeur, int pieces, decimal perte)
    {
        var action = () => _calculateurs.Surface(new CalculSurfaceRequete
        {
            Longueur = longueur, Largeur = largeur, NombrePieces = pieces, PourcentagePerte = perte
        });

        action.Should().Throw<BusinessRuleException>();
    }

    [Fact]
    public void Le_nombre_d_unites_est_arrondi_au_superieur()
    {
        var resultat = _calculateurs.Quantite(new CalculQuantiteRequete
        {
            QuantiteParUnite = 25m,
            QuantiteSouhaitee = 100m,
            PourcentagePerte = 5m
        });

        resultat.QuantiteNecessaire.Should().Be(100m);
        resultat.QuantiteAvecPerte.Should().Be(105m);
        // 105 / 25 = 4,2 → il faut acheter 5 unités entières.
        resultat.UnitesNecessaires.Should().Be(5);
    }

    [Fact]
    public void Une_quantite_qui_tombe_juste_ne_demande_pas_d_unite_supplementaire()
    {
        var resultat = _calculateurs.Quantite(new CalculQuantiteRequete
        {
            QuantiteParUnite = 25m, QuantiteSouhaitee = 100m, PourcentagePerte = 0m
        });

        resultat.UnitesNecessaires.Should().Be(4);
    }

    [Theory]
    [InlineData(0, 100, 5)]
    [InlineData(25, 0, 5)]
    [InlineData(25, 100, 101)]
    public void Une_saisie_incoherente_de_quantite_est_refusee(
        decimal parUnite, decimal souhaitee, decimal perte)
    {
        var action = () => _calculateurs.Quantite(new CalculQuantiteRequete
        {
            QuantiteParUnite = parUnite, QuantiteSouhaitee = souhaitee, PourcentagePerte = perte
        });

        action.Should().Throw<BusinessRuleException>();
    }
}
