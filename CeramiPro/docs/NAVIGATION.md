# Navigation de l'application

Le menu latéral suit le chemin réel de la matière dans l'atelier : de l'achat
de l'argile jusqu'à la facture remise au client.

```
🏠 Tableau de bord

📦 Stock
   ├── Vue générale
   ├── Matières premières
   ├── Produits finis
   ├── Mouvements
   └── Alertes

🏺 Produits
   ├── Catalogue
   ├── Catégories
   ├── Variantes
   └── Recettes

🏭 Production
   ├── Ordres de production
   ├── Planning
   ├── Production en cours
   └── Historique

🔥 Cuisson
   ├── Fours
   ├── Lots de cuisson
   └── Historique

🎨 Décoration
   ├── Travaux de décoration
   ├── Types de décoration
   └── Historique

🔍 Contrôle qualité
👥 Clients
📋 Commandes personnalisées
🚚 Fournisseurs
🛒 Ventes
💳 Paiements
📄 Factures
💸 Dépenses
📊 Rapports
⚙️ Paramètres
```

## Comment un écran s'affiche

```
Clic sur une entrée
        ↓
FenetrePrincipaleVueModele.NaviguerCommand
        ↓
ServiceNavigation.Naviguer(type de vue-modèle)
        ↓
La vue-modèle est construite par l'injection de dépendances
        ↓
ChargerAsync() lit les données
        ↓
Le modèle de données (DataTemplate) associe la vue-modèle à son écran XAML
```

La fenêtre principale ne connaît aucun écran individuellement : elle affiche
la vue-modèle courante, et WPF choisit l'écran correspondant.

## Droits

Chaque entrée peut exiger un droit. Une entrée interdite n'est pas seulement
grisée : elle disparaît, ainsi que le groupe qui la contenait s'il devient
vide. La vérification est refaite dans les services : masquer un bouton ne
protège rien.

## Entrées non encore actives

Une entrée sans écran développé n'ouvre rien plutôt que d'afficher une page
vide. À l'étape 1, seul le tableau de bord est actif ; les autres s'activent
au fil des étapes.
