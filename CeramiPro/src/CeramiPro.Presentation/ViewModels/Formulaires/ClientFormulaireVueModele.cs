using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Création et modification d'une fiche client.</summary>
public class ClientFormulaireVueModele : FormulaireVueModele<ClientRequete>
{
    private readonly IClientService _clients;

    public ClientFormulaireVueModele(IClientService clients) => _clients = clients;

    public override string Titre => EstCreation ? "Nouveau client" : "Modifier le client";

    public override IReadOnlyList<ChampFormulaire> Champs { get; } = new ChampFormulaire[]
    {
        new("Nom", nameof(ClientRequete.Nom), TypeChamp.Texte, Obligatoire: true),
        new("Téléphone", nameof(ClientRequete.Telephone), TypeChamp.Texte,
            Aide: "Le numéro WhatsApp du client, le plus souvent."),
        new("Instagram", nameof(ClientRequete.Instagram), TypeChamp.Texte,
            Aide: "Son compte, par exemple @atelier.nadia — un lien copié depuis Instagram convient aussi."),
        new("Email", nameof(ClientRequete.Email)),
        new("Adresse", nameof(ClientRequete.Adresse)),
        new("Ville", nameof(ClientRequete.Ville)),
        new("Notes", nameof(ClientRequete.Notes), TypeChamp.TexteLong),
        new("Client actif", nameof(ClientRequete.Actif), TypeChamp.Case)
    };

    public override async Task PreparerModificationAsync(int id)
    {
        var client = await _clients.ObtenirAsync(id);

        Id = id;
        Requete = new ClientRequete
        {
            Nom = client.Nom,
            Telephone = client.Telephone,
            Instagram = client.Instagram,
            Email = client.Email,
            Adresse = client.Adresse,
            Ville = client.Ville,
            Notes = client.Notes,
            Actif = client.Actif
        };
    }

    protected override async Task EnregistrerAsync()
    {
        if (Id is { } identifiant)
        {
            await _clients.ModifierAsync(identifiant, Requete);
        }
        else
        {
            await _clients.CreerAsync(Requete);
        }
    }
}
