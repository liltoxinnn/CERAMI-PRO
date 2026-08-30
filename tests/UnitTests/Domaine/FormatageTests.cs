using CeramicWorkshop.Application.Common;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.Domaine;

public class FormatageTests
{
    [Fact]
    public void Un_montant_est_affiche_au_format_algerien()
    {
        var texte = MontantFormatter.Formater(45000m);

        texte.Should().Be($"45{MontantFormatter.SeparateurMilliers}000,00 DA");
    }

    [Fact]
    public void Le_symbole_de_devise_est_configurable()
    {
        MontantFormatter.Formater(1234.5m, "€").Should().Be($"1{MontantFormatter.SeparateurMilliers}234,50 €");
    }

    [Fact]
    public void Le_nombre_de_decimales_est_configurable()
    {
        MontantFormatter.Formater(1850m, "DA", 0).Should().Be($"1{MontantFormatter.SeparateurMilliers}850 DA");
    }

    [Theory]
    [InlineData(20, "20")]
    [InlineData(1.5, "1,5")]
    [InlineData(0.125, "0,125")]
    public void Une_quantite_n_affiche_pas_de_decimales_inutiles(decimal quantite, string attendu)
    {
        MontantFormatter.FormaterQuantite(quantite).Should().Be(attendu);
    }

    [Fact]
    public void Une_quantite_peut_etre_suivie_de_son_unite()
    {
        MontantFormatter.FormaterQuantite(1.5m, "kg").Should().Be("1,5 kg");
    }

    [Fact]
    public void Une_date_est_affichee_en_jour_mois_annee()
    {
        MontantFormatter.FormaterDate(new DateTime(2026, 9, 15)).Should().Be("15/09/2026");
    }

    [Fact]
    public void Une_date_avec_heure_utilise_le_format_24_heures()
    {
        MontantFormatter.FormaterDateHeure(new DateTime(2026, 9, 15, 14, 30, 0)).Should().Be("15/09/2026 14:30");
    }
}
