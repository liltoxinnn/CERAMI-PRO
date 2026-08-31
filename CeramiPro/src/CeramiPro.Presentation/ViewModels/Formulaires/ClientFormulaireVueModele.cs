using CeramiPro.Application.DTOs.Commercial;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;

namespace CeramiPro.Presentation.ViewModels.Formulaires;

/// <summary>Création et modification d'une fiche client.</summary>
public class ClientFormulaireVueModele : FormulaireVueModele<ClientRequete>
{
    private readonly IClientService _clients;

    public ClientFormulaireVueModele(IClientService clients, IServiceLangue langue)
        : base(langue)
        => _clients = clients;

    public override string Titre => EstCreation ? "Nouveau client" : "Modifier le client";

    public override IReadOnlyList<ChampFormulaire> Champs { get; } = new ChampFormulaire[]
    {
        new("Nom", nameof(ClientRequete.Nom), TypeChamp.Texte, Obligatoire: true),
        new("Téléphone", nameof(ClientRequete.Telephone)),
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
