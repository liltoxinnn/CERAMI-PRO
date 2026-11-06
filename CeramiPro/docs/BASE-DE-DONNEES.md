# Base de données CeramiProDB

PostgreSQL. **19 tables** dans le schéma `public`, plus la table technique des
migrations d'Entity Framework (`__EFMigrationsHistory`). Sur une base venue de
l'ancienne version, un schéma `archives` garde en plus, en lecture seule, les
données des modules retirés (voir [MISE-A-JOUR.md](MISE-A-JOUR.md)).

## Arbre des tables

```
CeramiProDB (schéma public)
│
├── Comptes
│   └── Users                    compte, mot de passe haché, rôle (0 Employé, 1 Administrateur)
│
├── Clients
│   ├── Customers                fiche : téléphone, Instagram, adresse
│   └── CustomerNotes            historique des échanges
│
├── Produits
│   ├── ProductCategories        vases, assiettes, sculptures…
│   ├── Products                 prix, coût, stock, seuil, pièce unique
│   └── ProductImages            photos (chemin du fichier, photo principale)
│
├── Commandes personnalisées
│   ├── CustomOrders             canal, total, versé, reste, étape, date limite
│   ├── CustomOrderImages        modèle envoyé, croquis, photo de la pièce
│   └── CustomOrderNotes         suivi de la commande
│
├── Ventes et règlements
│   ├── Sales                    en-tête de la vente, total, payé, statut
│   ├── SaleItems                lignes : produit, quantité, prix, coût de revient
│   ├── Payments                 encaissements (vente ou commande), acomptes
│   └── PaymentMethods           espèces, virement, carte, chèque, autre
│
├── Stock
│   └── InventoryTransactions    chaque entrée, vente, casse, correction, annulation
│
├── Dépenses
│   ├── ExpenseCategories        électricité, gaz, transport, emballage…
│   └── Expenses                 date, montant, catégorie, mode de règlement
│
└── Réglages et journal
    ├── BusinessSettings         identité de l'atelier, mentions légales, préfixes
    ├── SystemSettings           sauvegarde automatique, stock négatif…
    └── AuditLogs                qui a fait quoi, quand
```

## Relations

| Table | Colonne | Vers | À la suppression |
|-------|---------|------|------------------|
| Products | ProductCategoryId | ProductCategories | refusée |
| ProductImages | ProductId | Products | supprimées avec le produit |
| CustomerNotes | CustomerId | Customers | supprimées avec le client |
| CustomOrders | CustomerId | Customers | refusée |
| CustomOrders | AssignedUserId | Users | mise à vide |
| CustomOrderImages, CustomOrderNotes | CustomOrderId | CustomOrders | supprimées avec la commande |
| Sales | CustomerId | Customers (facultatif) | refusée |
| SaleItems | SaleId | Sales | supprimées avec la vente |
| SaleItems | ProductId | Products | refusée |
| Payments | SaleId / CustomOrderId / CustomerId | Sales / CustomOrders / Customers | refusée |
| Payments | PaymentMethodId | PaymentMethods | refusée |
| InventoryTransactions | ProductId | Products | refusée |
| InventoryTransactions | SaleId / CustomOrderId | Sales / CustomOrders | refusée |
| InventoryTransactions | ReversedTransactionId | InventoryTransactions | refusée |
| Expenses | ExpenseCategoryId / PaymentMethodId | ExpenseCategories / PaymentMethods | refusée |
| Sales, Payments, Expenses, InventoryTransactions, AuditLogs, CustomerNotes, CustomOrderNotes | UserId | Users | mise à vide |

« Refusée » : la base empêche de supprimer une fiche encore utilisée. Le
logiciel propose alors de la désactiver.

## Règles de cohérence

* **Rien ne s'efface vraiment.** Ventes, commandes, paiements et dépenses
  portent `IsDeleted`, `DeletedAt` et `DeletionReason` : une annulation les
  retire des listes et des totaux, pas de la base.
* **Le stock ne change que par un mouvement.** `Products.CurrentStock` est
  toujours égal au dernier `QuantityAfter` de ses mouvements ; une annulation
  crée le mouvement inverse et pointe vers l'original
  (`ReversedTransactionId`).
* **Une opération, une transaction.** Une vente écrit son en-tête, ses lignes
  et ses sorties de stock en une seule écriture, puis son règlement, dans la
  même transaction : tout réussit ou rien.
* **Deux postes, pas d'écrasement silencieux.** Produits, ventes et commandes
  portent un jeton de version (`xmin` de PostgreSQL) : si une fiche a changé
  sur un autre poste entre la lecture et l'enregistrement, l'enregistrement
  est refusé avec un message clair.
* **Montants** en `numeric(18,2)`, **quantités** en `numeric(18,3)`, **dates**
  en UTC dans la base, affichées à l'heure d'Alger.
* **Pièce unique** : `Products.IsUniquePiece` limite le stock à 0 ou 1 ; elle
  ne peut pas être vendue deux fois, même si le stock négatif est autorisé.

## Migrations

| Migration | Rôle |
|-----------|------|
| `20260831052649_MigrationInitiale` | Schéma de l'ancienne version (49 tables) |
| `20261106111722_SimplificationDuSchema` | Passage à 19 tables, données conservées ou archivées |
| `20261106113933_PiecesUniquesEtCanauxDeCommande` | Pièce unique, canal de la commande, compte Instagram du client |

Elles s'appliquent toutes seules au démarrage du logiciel. Une installation
neuve les enchaîne ; une base de l'ancienne version ne reçoit que les deux
dernières.
