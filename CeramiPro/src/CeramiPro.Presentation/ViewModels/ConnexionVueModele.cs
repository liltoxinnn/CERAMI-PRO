using CeramiPro.Application.DTOs.Auth;
using CeramiPro.Application.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CeramiPro.Presentation.ViewModels;

/// <summary>
/// Écran de connexion. Le mot de passe n'est jamais conservé dans la
/// vue-modèle plus longtemps que nécessaire : il est transmis au service puis
/// effacé, y compris en cas d'échec.
/// </summary>
public partial class ConnexionVueModele : VueModeleBase
{
    private readonly IAuthService _auth;

    public ConnexionVueModele(IAuthService auth) => _auth = auth;

    public override string Titre => "Connexion";

    [ObservableProperty]
    private string _nomUtilisateur = string.Empty;

    [ObservableProperty]
    private string _motDePasse = string.Empty;

    /// <summary>Vrai quand la connexion a réussi : la fenêtre peut se fermer.</summary>
    [ObservableProperty]
    private bool _connexionReussie;

    /// <summary>Profil obtenu, lu par la fenêtre principale après la connexion.</summary>
    public UtilisateurConnecteDto? Profil { get; private set; }

    public string TitreEcran => "Connexion";
    public string LibelleNomUtilisateur => "Nom d'utilisateur";
    public string LibelleMotDePasse => "Mot de passe";
    public string LibelleConnexion => "Se connecter";
    public string NomApplication => "CeramiPro";
    public string SousTitreApplication => "Gestion d'atelier céramique";

    [RelayCommand]
    private async Task ConnecterAsync()
    {
        if (string.IsNullOrWhiteSpace(NomUtilisateur) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            MessageErreur = "Saisissez votre nom d'utilisateur et votre mot de passe.";
            return;
        }

        await ExecuterAsync(async () =>
        {
            var reponse = await _auth.ConnecterAsync(new ConnexionRequete
            {
                NomUtilisateur = NomUtilisateur,
                MotDePasse = MotDePasse
            });

            Profil = reponse.Utilisateur;
            ConnexionReussie = true;
        });

        // Le mot de passe ne survit pas à la tentative, réussie ou non.
        MotDePasse = string.Empty;
    }
}
