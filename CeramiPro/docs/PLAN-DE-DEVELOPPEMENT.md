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

## Étape 2 — Authentification et utilisateurs
Connexion, hachage des mots de passe, rôles, droits, session, journal d'audit.

## Étape 3 — Matières, fournisseurs, achats, stock
Fiches matières, unités, fournisseurs, commandes fournisseur, réceptions,
mouvements de stock et coût moyen pondéré.

## Étape 4 — Produits, variantes, images, recettes
Catalogue, catégories, variantes, photos, recettes de fabrication et calcul
automatique des besoins en matières.

## Étape 5 — Production
Ordres de fabrication, contrôle des matières avant lancement, tableau Kanban
des dix étapes, historique des transitions.

## Étape 6 — Séchage, fours, cuisson
Suivi du séchage, fiches fours, lots de cuisson, température, durée et coût
d'énergie réparti sur les pièces.

## Étape 7 — Décoration et contrôle qualité
Travaux de décoration, types, photos ; contrôle qualité obligatoire avant
qu'une pièce devienne un produit fini.

## Étape 8 — Clients et commandes personnalisées
Fiches clients, historique, commandes sur mesure avec dimensions, couleurs,
croquis, acompte et suivi jusqu'à la livraison.

## Étape 9 — Ventes, paiements, factures
Caisse rapide, remises, encaissement complet ou partiel, factures PDF au
format algérien.

## Étape 10 — Dépenses et rapports
Dépenses par catégorie, coût de revient réel, rapports de ventes, production,
stock et finances, export PDF et Excel.

## Étape 11 — Codes QR, codes-barres, impression
Génération des codes, lecture par douchette USB, étiquettes produits,
impression des factures, reçus et fiches de production.

## Étape 12 — Sauvegarde, sécurité, optimisation
Sauvegarde et restauration de la base, sauvegarde automatique, revue de
sécurité, optimisation des écrans les plus utilisés.

## Étape 13 — Tests, installateur, déploiement
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
