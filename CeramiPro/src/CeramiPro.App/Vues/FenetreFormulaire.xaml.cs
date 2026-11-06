using System.ComponentModel;
using System.Windows;
using CeramiPro.Presentation.ViewModels;

namespace CeramiPro.App.Vues;

/// <summary>
/// Fenêtre de saisie employée par tous les formulaires.
///
/// Les contrôles sont construits à partir des champs déclarés par la
/// vue-modèle, et liés directement à l'objet de requête : un seul fichier
/// XAML sert ainsi tous les modules, avec la même présentation.
/// </summary>
public partial class FenetreFormulaire : Window
{
    private INotifyPropertyChanged? _observee;

    public FenetreFormulaire()
    {
        InitializeComponent();
        DataContextChanged += (_, _) => Suivre();
    }

    /// <summary>
    /// S'abonne à la vue-modèle affichée, une seule fois : les champs se
    /// reconstruisent lorsqu'elle recharge ses listes déroulantes, et la
    /// fenêtre se ferme lorsque l'enregistrement a réussi.
    /// </summary>
    private void Suivre()
    {
        if (_observee is not null)
        {
            _observee.PropertyChanged -= SurChangement;
            _observee = null;
        }

        ConstruireChamps();

        if (DataContext is INotifyPropertyChanged observable)
        {
            _observee = observable;
            observable.PropertyChanged += SurChangement;
        }
    }

    private void SurChangement(object? source, PropertyChangedEventArgs args)
    {
        if (DataContext is not { } vueModele)
        {
            return;
        }

        switch (args.PropertyName)
        {
            case "Enregistre"
                when vueModele.GetType().GetProperty("Enregistre")?.GetValue(vueModele) is true:
                DialogResult = true;
                break;

            case "Champs":
                ConstruireChamps();
                break;
        }
    }

    private void ConstruireChamps()
    {
        ZoneChamps.Children.Clear();

        if (DataContext is not { } vueModele)
        {
            return;
        }

        var type = vueModele.GetType();

        if (type.GetProperty("Champs")?.GetValue(vueModele) is not IReadOnlyList<ChampFormulaire> champs
            || type.GetProperty("Requete")?.GetValue(vueModele) is not { } requete)
        {
            return;
        }

        ConstructeurChamps.Remplir(ZoneChamps, champs, requete);
    }
}
