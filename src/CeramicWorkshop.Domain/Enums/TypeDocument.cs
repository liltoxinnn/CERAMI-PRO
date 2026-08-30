using CeramicWorkshop.Domain.Common;

namespace CeramicWorkshop.Domain.Enums;

/// <summary>Documents numérotés automatiquement par le logiciel.</summary>
public enum TypeDocument
{
    [Libelle("Client")] Client,
    [Libelle("Fournisseur")] Fournisseur,
    [Libelle("Matière première")] Matiere,
    [Libelle("Lot de matière")] LotMatiere,
    [Libelle("Produit")] Produit,
    [Libelle("Achat")] Achat,
    [Libelle("Règlement fournisseur")] ReglementFournisseur,
    [Libelle("Ordre de production")] Production,
    [Libelle("Cuisson")] Cuisson,
    [Libelle("Décoration")] Decoration,
    [Libelle("Contrôle qualité")] Qualite,
    [Libelle("Commande personnalisée")] Commande,
    [Libelle("Vente")] Vente,
    [Libelle("Facture")] Facture,
    [Libelle("Paiement")] Paiement,
    [Libelle("Dépense")] Depense,
    [Libelle("Régularisation de stock")] Ajustement
}
