# Navigation de l'application

Le menu latéral suit le chemin réel de la matière dans l'atelier : de l'achat
de l'argile jusqu'à la facture remise au client.

Chaque entrée ouvre un écran ou déplie un groupe : aucune n'est sans effet.
Une entrée n'apparaît que si la personne connectée possède le droit qui lui
est attaché — et le service métier vérifie de son côté, l'écran n'étant jamais
la seule barrière.

```
🏠 Tableau de bord
🧾 Caisse

📦 Stock
   ├── Vue générale
   ├── Matières premières
   ├── Produits finis
   ├── Mouvements
   └── Alertes

🏺 Produits
   ├── Catalogue
   ├── Catégories
   ├── Recettes
   └── Étiquettes

🏭 Production
   ├── Tableau de production
   ├── Ordres de production
   ├── Production en cours
   └── Historique

🔥 Cuisson
   ├── Fours
   ├── Lots de cuisson
   └── Enfourner

🎨 Décoration
   ├── Travaux de décoration
   └── Types de décoration

🔍 Contrôle qualité
👥 Clients
📋 Commandes personnalisées

🚚 Fournisseurs
   ├── Fiches fournisseurs
   ├── Achats
   └── Nouvel achat

🛒 Ventes
📄 Factures
💳 Paiements
💸 Dépenses
📊 Rapports
🧮 Calculateurs

🛠️ Administration
   ├── Utilisateurs
   ├── Unités de mesure
   ├── Catégories de matières
   ├── Catégories de dépenses
   └── Sauvegarde

⚙️ Paramètres
```

---

## Trois écrans pour trente-huit destinations

Écrire un fichier XAML par module aurait donné trente-huit variantes
légèrement différentes, à corriger une par une. Trois écrans suffisent, parce
que ce sont les vues-modèles qui décrivent ce qu'il faut afficher.

| Écran | Sert à | Ce que la vue-modèle déclare |
|-------|--------|------------------------------|
| `VueListe` | 26 écrans de liste | `Colonnes`, `TypeFormulaire`, `Actions` |
| `VueDocument` | Caisse, nouvel achat, enfournement | `Champs`, `NomArticle`, `AfficherPrix`, `GereReglement` |
| `FenetreFormulaire` | 15 formulaires de saisie | `Champs`, `Requete` |

Les écrans qui ne ressemblent à aucun autre — tableau de bord, tableau de
production, alertes, rapports, étiquettes, sauvegarde, paramètres,
calculateurs, vue générale du stock — ont leur propre fichier.

### Une colonne

```csharp
new("Prix de vente", "PrixVente", ColonneAlignement.Droite, FormatColonne.Montant)
```

La colonne connaît sa mise en forme : un montant s'écrit `4 200,00 DA` à
l'écran, dans le classeur exporté et sur le document imprimé, sans que trois
codes différents aient à s'accorder.

### Un champ de saisie

```csharp
new("Catégorie", nameof(MatiereRequete.CategorieId), TypeChamp.Liste,
    Obligatoire: true, Options: categories)
```

Le champ est lié directement à l'objet de requête envoyé au service : il n'y a
pas de recopie intermédiaire où une valeur pourrait se perdre.

### Une action du métier

```csharp
new("Enregistrer la réception", ReceptionnerCommand,
    Aide: "Les matières commandées entrent en stock.")
```

Réceptionner un achat, défourner une fournée, annuler une vente : ces gestes
n'ont rien de commun, mais tous s'appliquent à la ligne choisie dans le
tableau. Les décrire ainsi permet à l'écran générique de les présenter sans
rien savoir du métier.

---

## Ce que chaque écran de liste sait faire

| Bouton | Présent quand |
|--------|---------------|
| Rechercher, Actualiser | toujours |
| Ajouter, Modifier | l'écran déclare un `TypeFormulaire` |
| Supprimer | l'écran redéfinit `PeutSupprimer` |
| Exporter vers Excel, Imprimer en PDF | toujours |
| Actions du métier | l'écran en déclare |

Un double-clic sur une ligne ouvre sa fiche. L'export porte sur la liste
entière, et non sur la seule page affichée : c'est ce que l'on attend d'un
export, et cela évite de parcourir les pages une par une.

---

## Deux langues

L'interface bascule du français à l'arabe sans redémarrer. Les entrées du menu
ne retiennent qu'une clé de traduction, jamais un texte figé, et la fenêtre
entière s'inverse : le menu passe à droite, les colonnes et les champs suivent.

Une clé sans traduction arabe retombe sur le français, ce qui vaut mieux qu'un
écran vide ou qu'un nom technique affiché tel quel.
