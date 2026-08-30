using CeramicWorkshop.Infrastructure.Authentication;
using FluentAssertions;

namespace CeramicWorkshop.UnitTests.Services;

public class HachageMotDePasseTests
{
    private readonly PasswordHasherService _hachage = new();

    [Fact]
    public void Le_mot_de_passe_n_est_jamais_stocke_en_clair()
    {
        const string motDePasse = "Atelier@2026";

        var empreinte = _hachage.Hacher(motDePasse);

        empreinte.Should().NotContain(motDePasse);
        empreinte.Should().StartWith("v1.");
    }

    [Fact]
    public void Deux_hachages_du_meme_mot_de_passe_different_grace_au_sel()
    {
        var premiere = _hachage.Hacher("Atelier@2026");
        var seconde = _hachage.Hacher("Atelier@2026");

        premiere.Should().NotBe(seconde);
    }

    [Fact]
    public void Le_bon_mot_de_passe_est_accepte()
    {
        var empreinte = _hachage.Hacher("Atelier@2026");

        _hachage.Verifier("Atelier@2026", empreinte).Should().BeTrue();
    }

    [Theory]
    [InlineData("atelier@2026")]
    [InlineData("Atelier@2027")]
    [InlineData("")]
    public void Un_mot_de_passe_incorrect_est_refuse(string tentative)
    {
        var empreinte = _hachage.Hacher("Atelier@2026");

        _hachage.Verifier(tentative, empreinte).Should().BeFalse();
    }

    [Theory]
    [InlineData("empreinte-invalide")]
    [InlineData("v2.1000.AAAA.BBBB")]
    [InlineData("v1.pas-un-nombre.AAAA.BBBB")]
    public void Une_empreinte_corrompue_ne_fait_pas_echouer_l_application(string empreinte)
    {
        _hachage.Verifier("Atelier@2026", empreinte).Should().BeFalse();
    }
}
