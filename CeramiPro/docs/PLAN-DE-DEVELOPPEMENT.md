# Plan de développement

Treize étapes. Une étape n'est close que lorsque le projet compile, que les
tests passent, que les migrations s'appliquent sur une base vierge et que les
écrans concernés fonctionnent réellement.

Aucune étape ne laisse derrière elle un bouton sans effet, un écran vide, une
fonctionnalité simulée ou des données inventées.

---

## Étape 1 — Fondations · **terminée**

Solution, projets, WPF, MVVM, injection de dépendances, PostgreSQL, EF Core,
contexte de données, configuration, journalisation.

## Étape 2 — Authentification et utilisateurs · **terminée**
Connexion, hachage des mots de passe, rôles, droits, session, journal d'audit.

## Étape 3 — Matières, fournisseurs, achats, stock · **terminée**
Fiches matières, unités, fournisseurs, commandes fournisseur, réceptions,
mouvements de stock et coût moyen pondéré.

## Étape 4 — Produits, variantes, images, recettes · **terminée**
Catalogue, catégories, variantes, photos, recettes de fabrication et calcul
automatique des besoins en matières.

## Étape 5 — Production · **terminée**
Ordres de fabrication, contrôle des matières avant lancement, tableau Kanban
des dix étapes, historique des transitions.

## Étape 6 — Séchage, fours, cuisson · **terminée**
Suivi du séchage, fiches fours, lots de cuisson, température, durée et coût
d'énergie réparti sur les pièces.

## Étape 7 — Décoration et contrôle qualité · **terminée**
Travaux de décoration, types, photos ; contrôle qualité obligatoire avant
qu'une pièce devienne un produit fini.

## Étape 8 — Clients et commandes personnalisées · **terminée**
Fiches clients, historique, commandes sur mesure avec dimensions, couleurs,
croquis, acompte et suivi jusqu'à la livraison.

## Étape 9 — Ventes, paiements, factures · **terminée**
Caisse rapide, remises, encaissement complet ou partiel, factures PDF au
format algérien.

## Étape 10 — Dépenses et rapports · **terminée**
Dépenses par catégorie, coût de revient réel, rapports de ventes, production,
stock et finances, export PDF et Excel.

## Étape 11 — Codes QR, codes-barres, impression · **terminée**
Génération des codes, lecture par douchette USB, étiquettes produits,
impression des factures, reçus et fiches de production.

## Étape 12 — Sauvegarde, sécurité, optimisation · **terminée**
Sauvegarde et restauration de la base, sauvegarde automatique, revue de
sécurité, optimisation des écrans les plus utilisés.

## Étape 13 — Tests, installateur, déploiement · **terminée**
Tests de bout en bout, installateur Windows, raccourci bureau, documentation
d'installation et guide d'utilisation.

---

## Vérifications à la fin de chaque étape

1. Le projet compile sans erreur.
2. Les tests passent.
3. PostgreSQL répond et les migrations s'appliquent sur une base vierge.
4. Les relations entre les tables sont correctes.
5. Les règles de gestion concernées sont couvertes par des tests.
6. Les écrans WPF s'ouvrent et fonctionnent.
7. Les erreurs affichent un message français, jamais une trace technique.
8. Aucun bouton sans effet, aucun écran vide, aucune donnée inventée.

---

## Ce que la compilation ne vérifie pas

WPF ne se compile que sous Windows. Trois vérifications remplacent ici ce que
le compilateur XAML aurait signalé, et rattrapent ce qu'aucun compilateur ne
voit de toute façon.

### Le vérificateur XAML

```bat
python outils\verifier-xaml.py
```

Il contrôle, pour chacun des vingt fichiers XAML : le XML bien formé, la
classe partielle compagnon, chaque `{StaticResource}` employé, chaque
gestionnaire d'événement nommé, et chaque `{Binding ...Command}` — une
commande mal orthographiée resterait sans effet, en silence, jusqu'au clic.

### Les écrans déclarés

Les écrans et les formulaires sont enregistrés dans `InjectionPresentation`,
au sein de la couche présentation, plutôt que dans l'application Windows.
C'est ce qui permet aux tests de vérifier que chaque destination du menu peut
réellement être construite. Un écran oublié dans l'injection de dépendances ne
se voit ni à la compilation, ni dans les tests métier : il n'échoue qu'au clic,
devant l'utilisateur.

Cette vérification a immédiatement révélé que les outils communs aux listes
retenaient un service à durée de vie plus courte — une faute qui aurait fermé
le logiciel au démarrage.

### Les colonnes et les champs

Les tests relisent la description de chaque écran : une colonne doit viser une
propriété qui existe, un champ de saisie une propriété de sa requête, et une
colonne chiffrée doit être alignée à droite. Une liaison vers une propriété
absente n'affiche rien et ne signale rien.
