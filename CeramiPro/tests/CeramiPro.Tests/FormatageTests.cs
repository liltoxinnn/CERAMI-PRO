using CeramiPro.Application.Common;
using FluentAssertions;

namespace CeramiPro.Tests;

/// <summary>
/// Les montants, quantités et dates doivent s'afficher au format algérien,
/// tel que l'atelier les écrit à la main.
///
/// Les espaces attendues sont écrites en clair (  pour les milliers,
///   devant une unité) : une espace ordinaire dans un document imprimé
/// laisserait la devise passer à la ligne suivante.
/// </summary>
public class FormatageTests
{
    private const string Fine = "\u202F";
    private const string Insecable = "\u00A0";

    [Fact]
    public void Un_montant_est_affiche_en_dinars()
        => Formatage.Montant(45000).Should().Be($"45{Fine}000,00{Insecable}DA");

    [Fact]
    public void Un_montant_nul_reste_lisible()
        => Formatage.Montant(0).Should().Be($"0,00{Insecable}DA");

    [Fact]
    public void Les_centimes_sont_toujours_affiches()
        => Formatage.Montant(1234.5m).Should().Be($"1{Fine}234,50{Insecable}DA");

    [Fact]
    public void Un_montant_negatif_garde_son_signe()
        => Formatage.Montant(-2500).Should().Be($"-2{Fine}500,00{Insecable}DA");

    [Fact]
    public void Un_grand_montant_groupe_les_milliers()
        => Formatage.Montant(125000.75m).Should().Be($"125{Fine}000,75{Insecable}DA");

    [Fact]
    public void Une_quantite_entiere_n_affiche_pas_de_decimale()
        => Formatage.Quantite(3).Should().Be("3");

    [Fact]
    public void Une_quantite_decimale_garde_sa_precision()
        => Formatage.Quantite(1.5m, "kg").Should().Be($"1,5{Insecable}kg");

    [Fact]
    public void Une_quantite_inferieure_a_un_est_correcte()
        => Formatage.Quantite(0.25m, "L").Should().Be($"0,25{Insecable}L");

    [Fact]
    public void Une_unite_est_separee_par_une_espace_insecable()
        => Formatage.Quantite(20, "pièces").Should().Be($"20{Insecable}pièces");

    [Fact]
    public void Une_date_suit_le_format_francais()
        => Formatage.Date(new DateTime(2026, 8, 31)).Should().Be("31/08/2026");

    [Fact]
    public void Une_date_avec_heure_est_lisible()
        => Formatage.DateHeure(new DateTime(2026, 8, 31, 14, 30, 0)).Should().Be("31/08/2026 14:30");

    [Fact]
    public void Un_pourcentage_est_suivi_du_signe()
        => Formatage.Pourcentage(12.5m).Should().Be($"12,5{Insecable}%");

    [Fact]
    public void Un_pourcentage_entier_n_a_pas_de_decimale()
        => Formatage.Pourcentage(10).Should().Be($"10{Insecable}%");

    [Fact]
    public void La_devise_et_le_pays_sont_ceux_de_l_Algerie()
    {
        ParametresAtelier.CodeDevise.Should().Be("DZD");
        ParametresAtelier.SymboleDevise.Should().Be("DA");
        ParametresAtelier.CodePays.Should().Be("DZ");
        ParametresAtelier.FuseauHoraire.Should().Be("Africa/Algiers");
        ParametresAtelier.NomBaseDeDonnees.Should().Be("CeramiProDB");
    }
}
