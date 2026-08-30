# Plan de développement

Le logiciel est construit étape par étape. Une étape n'est considérée terminée que
lorsque le projet compile, que les tests passent, que les migrations s'appliquent
et que les écrans correspondants fonctionnent réellement.

---

## Étape 1 — Fondations · **terminée**

Architecture, base de données et sécurité.

* Solution en cinq projets suivant une architecture en couches.
* 45 entités, 22 énumérations, 49 tables PostgreSQL, 91 clés étrangères.
* Migration initiale appliquée et vérifiée.
* Connexion par jeton JWT, hachage PBKDF2, blocage après cinq échecs.
* 45 droits répartis en 9 modules, 4 rôles préconfigurés.
* Écrans : connexion, tableau de bord, utilisateurs, rôles et droits,
  paramètres de l'atelier, mon compte.
* Journal des opérations, suppression logique, dates en UTC.
* 63 tests unitaires et 13 tests d'intégration.

## Étape 2 — Stock

* Catégories de matières, unités de mesure personnalisées.
* Fiche matière complète : seuils, coût moyen, emplacement, photo.
* Fournisseurs : coordonnées, historique, total acheté, payé, restant.
* Achats : lignes, réception, lots de matière, règlements fournisseurs.
* Mouvements de stock avec stock avant et après, écran de consultation.
* Régularisations d'inventaire justifiées.
* Alertes de stock faible.

## Étape 3 — Produits

* Catégories et catalogue produit avec fiche détaillée.
* Photos multiples : principale, supplémentaires, fabrication, produit terminé.
* Variantes (taille, couleur) avec stock propre.
* Recettes de fabrication et calcul automatique des besoins pour une série.
* Calcul du coût de production prévisionnel.

## Étape 4 — Production

* Ordres de production, priorité, employé responsable, dates prévues et réelles.
* Vérification des matières disponibles avant lancement, avec message explicite.
* Consommation des matières et mouvements associés.
* Tableau de production visuel par étape (planifié → terminé).
* Historique daté de chaque étape avec quantités acceptées et endommagées.
* Entrée en stock des produits finis à la clôture.

## Étape 5 — Cuisson, décoration, qualité

* Fours : capacité, températures, état.
* Fournées : température, durée, coût énergétique, pièces enfournées.
* Décoration : type, couleurs, émail, peinture, or, argent, photos.
* Contrôle qualité sur huit points, défauts, gravité, solution.
* Blocage du passage à « Terminé » sans contrôle qualité conforme.

## Étape 6 — Clients et commandes personnalisées

* Fiche client : historique, factures, paiements, montant restant.
* Commandes personnalisées : dimensions, couleurs, matériaux, date limite.
* Photos de référence, croquis, photos de fabrication.
* Suivi du statut de la commande jusqu'à la livraison.
* Signalement des commandes proches de l'échéance ou en retard.

## Étape 7 — Ventes, factures, paiements

* Écran de vente rapide : client, produits, quantité, remise, total.
* Diminution automatique du stock et mouvements associés.
* Factures et reçus PDF en français, au format algérien.
* Paiements complets, partiels, acomptes et règlements de dettes.
* Suivi des dettes clients et fournisseurs.

## Étape 8 — Gestion financière

* Dépenses par catégorie, avec justificatif.
* Calcul du coût réel de production : matières, main-d'œuvre, cuisson,
  décoration, emballage, autres.
* Tableau de bord complet : chiffre d'affaires, bénéfice, production en cours.
* Graphiques : ventes par jour et par mois, produits les plus vendus et les plus rentables,
  consommation des matières, production mensuelle.
* Rapports exportables en PDF et Excel.
* Calculateurs : matières, surface avec pourcentage de perte, quantités.

## Étape 9 — Codes-barres et QR codes

* Génération des codes pour chaque produit.
* Lecture par caméra de téléphone et par lecteur USB.
* Le scan ouvre la fiche produit, ajoute à une vente ou ouvre une production.

## Étape 10 — Finalisation

* Recherche globale tolérante aux fautes de frappe.
* Notifications regroupées et réglages associés.
* Sauvegarde et restauration de la base par l'administrateur.
* Revue de sécurité, optimisation, vérification sur téléphone et tablette.
* Documentation d'installation et guide d'utilisation.

---

## Vérifications à la fin de chaque étape

1. Le projet compile sans erreur ni avertissement.
2. Les tests unitaires et d'intégration passent.
3. Les migrations PostgreSQL s'appliquent sur une base vierge.
4. Les relations entre les tables sont vérifiées.
5. Les règles métier concernées sont couvertes par des tests.
6. Les écrans sont utilisables au clavier, sur ordinateur et sur téléphone.
7. Aucun bouton sans effet, aucune page vide, aucune donnée fictive.
