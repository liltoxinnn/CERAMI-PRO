using CeramiPro.Application.DTOs.Stock;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Création et modification d'une fiche fournisseur.</summary>
public class FournisseurFormulaireVueModele : FormulaireVueModele<FournisseurRequete>
{
    private readonly IFournisseurService _fournisseurs;

    public FournisseurFormulaireVueModele(IFournisseurService fournisseurs, IServiceLangue langue)
        : base(langue)
        => _fournisseurs = fournisseurs;

    public override string Titre => EstCreation ? "Nouveau fournisseur" : "Modifier le fournisseur";

    public override IReadOnlyList<ChampFormulaire> Champs { get; } = new ChampFormulaire[]
    {
        new("Nom du contact", nameof(FournisseurRequete.Nom), TypeChamp.Texte, Obligatoire: true),
        new("Entreprise", nameof(FournisseurRequete.Entreprise)),
        new("Téléphone", nameof(FournisseurRequete.Telephone)),
        new("Email", nameof(FournisseurRequete.Email)),
        new("Adresse", nameof(FournisseurRequete.Adresse)),
        new("Ville", nameof(FournisseurRequete.Ville)),
        new("Notes", nameof(FournisseurRequete.Notes), TypeChamp.TexteLong),
        new("Fournisseur actif", nameof(FournisseurRequete.Actif), TypeChamp.Case)
    };

    public override async Task PreparerModificationAsync(int id)
    {
        var fournisseur = await _fournisseurs.ObtenirAsync(id);

        Id = id;
        Requete = new FournisseurRequete
        {
            Nom = fournisseur.Nom,
            Entreprise = fournisseur.Entreprise,
            Telephone = fournisseur.Telephone,
            Email = fournisseur.Email,
            Adresse = fournisseur.Adresse,
            Ville = fournisseur.Ville,
            Notes = fournisseur.Notes,
            Actif = fournisseur.Actif
        };
    }

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _fournisseurs.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _fournisseurs.CreerAsync(Requete);
        }
    }
}
