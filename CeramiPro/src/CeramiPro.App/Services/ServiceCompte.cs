using System.Windows;
using CeramiPro.App.Vues;
using CeramiPro.Application.Interfaces;
using CeramiPro.Presentation.Navigation;
using CeramiPro.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace CeramiPro.App.Services;

/// <summary>
/// Changement de mot de passe et fermeture de session, depuis le bas du menu
/// latéral.
///
/// La déconnexion referme la fenêtre principale : c'est le démarrage de
/// l'application qui redemandera les identifiants, comme au premier
/// lancement. Une seule marche à suivre vaut mieux que deux.
/// </summary>
public class ServiceCompte : IServiceCompte
{
    private readonly IServiceProvider _services;
    private readonly IServiceDialogue _dialogue;

    public ServiceCompte(IServiceProvider services, IServiceDialogue dialogue)
    {
        _services = services;
        _dialogue = dialogue;
    }

    public void ChangerMotDePasse()
    {
        // La fenêtre vit dans sa propre portée : son contexte de base de
        // données est refermé dès qu'elle se ferme.
        using var portee = _services.CreateScope();

        var vueModele = portee.ServiceProvider.GetRequiredService<ChangementMotDePasseVueModele>();
        vueModele.ProposeAuDemarrage = false;

        var fenetre = new FenetreMotDePasse
        {
            Owner = System.Windows.Application.Current.MainWindow,
            DataContext = vueModele
        };

        if (fenetre.ShowDialog() == true)
        {
            _dialogue.Succes("Votre mot de passe a été changé.");
        }
    }

    public void Deconnecter()
    {
        if (!_dialogue.Confirmer(
                "Fermer la session ?\n\n"
                + "Le logiciel se refermera : relancez-le pour qu'une autre personne "
                + "puisse se connecter."))
        {
            return;
        }

        // La session est fermée avant la fenêtre : les droits ne survivent
        // pas à la déconnexion, même le temps de l'extinction.
        //
        // Le service d'authentification vit le temps d'une opération : le
        // retenir dans ce service, qui dure autant que l'application, lui
        // ferait garder un contexte de base de données ouvert en permanence.
        using (var portee = _services.CreateScope())
        {
            portee.ServiceProvider.GetRequiredService<IAuthService>()
                .DeconnecterAsync().GetAwaiter().GetResult();
        }

        System.Windows.Application.Current.MainWindow?.Close();
    }
}
