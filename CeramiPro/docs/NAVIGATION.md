# Navigation de l'application

Sept sections, pas une de plus. Chaque entrée ouvre un écran ou déplie une
section : aucune n'est sans effet, et aucun écran n'est atteignable par deux
entrées différentes (des tests le vérifient).

```
🏠 Tableau de bord

🏺 Produits
   ├── 📖 Catalogue
   ├── 📦 Stock
   ├── 🔄 Historique du stock
   └── 🗂️ Catégories

📋 Commandes

👥 Clients

🛒 Ventes
   ├── 🧾 Nouvelle vente
   ├── 📜 Historique des ventes
   └── 💳 Paiements

📊 Rapports
   ├── 📈 Rapports
   └── 💸 Dépenses

⚙️ Paramètres
   ├── 🏛️ Atelier
   ├── 🗂️ Catégories de dépenses
   ├── 👤 Utilisateurs          administrateur seulement
   └── 💾 Sauvegarde            administrateur seulement
```

Le bas du menu affiche la personne connectée et son rôle, avec *Changer mon
mot de passe* et *Se déconnecter*. Le menu se réduit à ses icônes.

---

## Les écrans de liste

Catalogue, Stock, Historique du stock, Commandes, Clients, Historique des
ventes, Paiements, Dépenses, Catégories et Utilisateurs suivent le même
modèle :

| Élément | Rôle |
|---------|------|
| Recherche | Filtre sur le nom, la référence, le numéro, le téléphone… |
| Filtres | Propres à l'écran : stock faible, ventes à crédit, étape et canal des commandes |
| Tableau | Colonnes alignées : montants et quantités à droite |
| Ajouter / Modifier | Ouvre le formulaire de saisie, quand l'écran en a un |
| Actions | Les gestes du métier sur la ligne choisie (voir ci-dessous) |
| Exporter | Excel ou PDF de ce qui est affiché |
| Pagination | 25 lignes par page |

Une action sans ligne choisie le dit au lieu d'échouer. Une action qui défait
quelque chose (annuler une vente, une commande, un paiement, supprimer une
dépense) demande un motif, conservé dans l'historique.

### Actions par écran

| Écran | Actions |
|-------|---------|
| Catalogue | Photos (ajouter, choisir la principale, retirer) |
| Stock | Entrée en stock, Casse ou perte, Corriger après comptage |
| Commandes | Encaisser un acompte, Étape suivante, Marquer comme livrée, Reçu, Photos, Nouveau client, Annuler |
| Clients | Fiche du client (achats, commandes, paiements, notes) |
| Historique des ventes | Réimprimer le reçu, Encaisser le reste, Annuler la vente |
| Paiements | Reçu, Annuler le paiement |
| Dépenses | Supprimer la dépense |
| Utilisateurs | Désactiver ou réactiver, Nouveau mot de passe |

---

## Les écrans particuliers

| Écran | Fonctionnement |
|-------|----------------|
| **Tableau de bord** | Indicateurs du jour et du mois, commandes à surveiller, produits à refabriquer, graphiques des ventes sur 30 jours et 12 mois ; raccourcis *Nouvelle vente*, *Nouvelle commande*, *Nouveau produit*, et liens vers les commandes, le stock et les rapports |
| **Nouvelle vente** | On tape le début du nom ou de la référence, la quantité vaut 1, le prix du catalogue est proposé ; le rendu de monnaie s'affiche pendant la saisie du montant reçu ; le reçu s'imprime à la validation. Une pièce unique vendue n'est plus proposée |
| **Rapports** | Choix du rapport et de la période (aujourd'hui, ce mois, le mois dernier, cette année, ou dates libres), tableau, graphique, totaux, export |
| **Atelier** (Paramètres) | Identité et mentions légales imprimées sur les reçus, monnaie, TVA, préfixes de numérotation, état de la base de données |
| **Sauvegarde** | Sauvegarder maintenant, copier sur une clé USB, restaurer, supprimer, purger ; réglage de la sauvegarde quotidienne |

---

## Formulaires

Une seule fenêtre de saisie sert à tous les formulaires : produit, client,
commande, encaissement, dépense, mouvement de stock, correction, catégorie,
compte. Un champ obligatoire resté vide est nommé dans le message ; une règle
métier refusée s'affiche telle que le service l'a rédigée, en français, sans
détail technique.
