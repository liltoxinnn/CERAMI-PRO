# Base de données CeramiProDB

PostgreSQL. 49 tables réparties en 20 groupes, plus la table technique des
migrations d'Entity Framework.

## Arbre des tables

```
CeramiProDB
│
├── Sécurité et comptes
│   ├── Users                    comptes, mot de passe haché, état
│   ├── Roles                    Administrateur, Responsable, Employé, Caissier
│   ├── Permissions              droits élémentaires
│   └── RolePermissions          droits accordés à chaque rôle
│
├── Clients
│   ├── Customers                fiche client
│   └── CustomerNotes            historique des échanges
│
├── Fournisseurs
│   ├── Suppliers                fiche fournisseur
│   └── SupplierPayments         règlements versés
│
├── Matières premières
│   ├── MaterialCategories       argile, émail, pigment, emballage…
│   ├── Units                    kg, g, L, ml, pièce, boîte
│   ├── Materials                fiche matière, stock, coût moyen
│   └── MaterialBatches          lots reçus, avec leur prix d'achat
│
├── Achats
│   ├── Purchases                commande fournisseur
│   └── PurchaseItems            lignes de la commande
│
├── Catalogue
│   ├── ProductCategories        vases, assiettes, sculptures…
│   ├── Products                 fiche produit, prix, coût, stock
│   ├── ProductVariants          couleur, taille, finition
│   └── ProductImages            photos du produit
│
├── Recettes
│   ├── ProductRecipes           recette de fabrication d'un produit
│   └── ProductRecipeItems       matières et quantités par pièce
│
├── Production
│   ├── ProductionOrders         ordre de fabrication
│   ├── ProductionMaterials      matières réellement consommées
│   └── ProductionStageHistory   passage d'une étape à l'autre
│
├── Cuisson
│   ├── Kilns                    fours, capacité, plage de température
│   ├── FiringBatches            lot de cuisson, durée, coût d'énergie
│   └── FiringBatchItems         pièces enfournées
│
├── Décoration
│   ├── DecorationTypes          peinture, émail, dorure, gravure…
│   ├── DecorationOrders         travail de décoration
│   └── DecorationImages         photos du décor
│
├── Qualité
│   ├── QualityChecks            contrôle d'une production
│   └── QualityIssues            défauts relevés
│
├── Commandes personnalisées
│   ├── CustomOrders             demande sur mesure d'un client
│   ├── CustomOrderImages        croquis, références, photos d'avancement
│   └── CustomOrderNotes         échanges avec le client
│
├── Ventes
│   ├── Sales                    vente au comptoir
│   └── SaleItems                lignes vendues
│
├── Paiements
│   ├── PaymentMethods           espèces, carte, virement, autre
│   └── Payments                 encaissements et acomptes
│
├── Facturation
│   ├── Invoices                 facture
│   └── InvoiceItems             lignes facturées
│
├── Stock
│   ├── InventoryTransactions    tout mouvement, sans exception
│   └── StockAdjustments         régularisation, avec son motif
│
├── Dépenses
│   ├── ExpenseCategories        électricité, gaz, transport, salaires…
│   └── Expenses                 dépense enregistrée
│
├── Alertes
│   ├── Notifications            alertes ouvertes
│   └── NotificationSettings     réglage de chaque type d'alerte
│
├── Traçabilité
│   └── AuditLogs                journal des opérations importantes
│
└── Paramètres
    ├── BusinessSettings         identité de l'atelier, devise, préfixes
    └── SystemSettings           réglages techniques
```

---

## Relations principales

```
Customer ────┬──< Sales ──────< SaleItems
             ├──< Payments
             ├──< Invoices ────< InvoiceItems
             ├──< CustomOrders ─< CustomOrderImages
             │                  └< CustomOrderNotes
             └──< CustomerNotes

Supplier ────┬──< Purchases ───< PurchaseItems
             └──< SupplierPayments

Material ────┬──< MaterialBatches
             ├──< PurchaseItems
             ├──< ProductRecipeItems
             ├──< ProductionMaterials
             └──< InventoryTransactions

Product ─────┬──< ProductVariants
             ├──< ProductImages
             ├──< ProductRecipes ──< ProductRecipeItems
             ├──< ProductionOrders
             ├──< SaleItems
             └──< InventoryTransactions

ProductionOrder ─┬──< ProductionMaterials
                 ├──< ProductionStageHistory
                 ├──< FiringBatchItems
                 ├──< DecorationOrders
                 └──< QualityChecks ──< QualityIssues

Kiln ────────────< FiringBatches ────< FiringBatchItems

Sale ────────────┬──< SaleItems
                 ├──< Payments
                 └──── Invoice   (une facture par vente)

Role ────────────< RolePermissions >──── Permission
User ────────────> Role
```

Lecture : `A ──< B` signifie « un A possède plusieurs B ».

---

## Conventions

- **Clé primaire** : `Id`, entier auto-incrémenté.
- **Traçabilité** : toute table métier porte `CreatedAt`, `CreatedByUserId`,
  `UpdatedAt`, `UpdatedByUserId`, remplis automatiquement.
- **Suppression logique** : les pièces comptables — ventes, factures,
  paiements, dépenses, achats — ne sont jamais effacées. Elles portent
  `IsDeleted`, `DeletedAt`, `DeletedByUserId` et `DeletionReason`, et
  disparaissent des écrans sans quitter la base.
- **Dates** : stockées en temps universel, affichées à l'heure d'Alger.
- **Montants** : `decimal(18,4)`, pour que les coûts de revient ne dérivent pas.
- **Numérotation** : `PRÉFIXE-ANNÉE-0000`, par exemple `CMD-2026-0042`.
