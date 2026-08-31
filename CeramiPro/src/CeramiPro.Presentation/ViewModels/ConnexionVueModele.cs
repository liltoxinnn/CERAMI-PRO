using CeramiPro.Application.DTOs.Auth;
using CeramiPro.Application.Interfaces;
using CeramiPro.Application.Localisation;
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
    private readonly IServiceLangue _langue;

    public ConnexionVueModele(IAuthService auth, IServiceLangue langue)
    {
        _auth = auth;
        _langue = langue;
        _langue.LangueChangee += RafraichirTextes;
    }

    public override string Titre => _langue["connexion.titre"];

    [ObservableProperty]
    private string _nomUtilisateur = string.Empty;

    [ObservableProperty]
    private string _motDePasse = string.Empty;

    /// <summary>Vrai quand la connexion a réussi : la fenêtre peut se fermer.</summary>
    [ObservableProperty]
    private bool _connexionReussie;

    /// <summary>Profil obtenu, lu par la fenêtre principale après la connexion.</summary>
    public UtilisateurConnecteDto? Profil { get; private set; }

    public IReadOnlyList<Langue> Langues { get; } = new[] { Langue.Francais, Langue.Arabe };

    public SensEcriture Sens => _langue.Sens;

    public string TitreEcran => _langue["connexion.titre"];
    public string LibelleNomUtilisateur => _langue["connexion.nomUtilisateur"];
    public string LibelleMotDePasse => _langue["connexion.motDePasse"];
    public string LibelleConnexion => _langue["action.connexion"];
    public string NomApplication => _langue["app.nom"];
    public string SousTitreApplication => _langue["app.sousTitre"];

    [RelayCommand]
    private async Task ConnecterAsync()
    {
        if (string.IsNullOrWhiteSpace(NomUtilisateur) || string.IsNullOrWhiteSpace(MotDePasse))
        {
            MessageErreur = _langue["etat.obligatoire"];
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

    [RelayCommand]
    private void ChoisirLangue(Langue langue) => _langue.Changer(langue);

    private void RafraichirTextes()
    {
        OnPropertyChanged(nameof(Titre));
        OnPropertyChanged(nameof(Sens));
        OnPropertyChanged(nameof(TitreEcran));
        OnPropertyChanged(nameof(LibelleNomUtilisateur));
        OnPropertyChanged(nameof(LibelleMotDePasse));
        OnPropertyChanged(nameof(LibelleConnexion));
        OnPropertyChanged(nameof(NomApplication));
        OnPropertyChanged(nameof(SousTitreApplication));
    }
}
