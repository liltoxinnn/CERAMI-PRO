# Remaniement de CeramiPro

Objectif : faire de CeramiPro un logiciel Windows **simple et fiable** pour un
atelier de céramique artisanale, en sept sections, sur une base de données
propre — sans perdre aucune donnée déjà saisie.

## 1. Audit de départ

L'application d'origine couvrait toute une fabrique : 19 entrées de menu,
49 tables, quatre rôles et des dizaines de droits, deux langues. Pour un
atelier de deux ou trois personnes, cela se traduisait par :

* des écrans jamais utilisés (recettes, ordres de production, fours,
  enfournement, décoration, contrôle qualité, achats fournisseurs,
  calculateurs, étiquettes) qui encombraient le menu et chaque fiche ;
* des notions qui ne correspondent pas à la façon de travailler de l'atelier
  (variantes de produits, factures distinctes des ventes, dix étapes pour une
  commande, matières premières suivies au gramme) ;
* et des défauts réels, relevés ci-dessous au § 5.

## 2. Ce qui a été retiré

| Retiré | Pourquoi | Ce que deviennent les données |
|--------|----------|-------------------------------|
| Recettes de fabrication, ordres et tableau de production | Trop lourd pour des pièces faites main ; le coût d'une pièce se saisit directement | Archivées |
| Fours, cuissons, enfournement, coût d'énergie réparti | Idem ; l'électricité et le gaz se saisissent en dépenses | Archivées |
| Décoration (types, travaux) | Idem | Archivées |
| Contrôle qualité | La casse se saisit en un geste : *Stock → Casse ou perte* | Archivées |
| Matières premières, unités, lots | L'atelier n'a pas besoin de suivre l'argile au gramme ; les achats se saisissent en dépenses | Archivées, mouvements compris |
| Fournisseurs, achats, réceptions, dettes fournisseurs | Hors du besoin exprimé | Archivées ; règlements fournisseurs marqués annulés |
| Factures | Le reçu de vente (80 mm, mentions légales) suffit à l'atelier | Archivées ; les paiements sont rattachés à la vente |
| Variantes de produits | Une variante devient un produit, ou s'additionne au stock du produit | Stock reporté, table archivée |
| Codes-barres, QR codes, douchette, étiquettes | Des pièces uniques ne s'étiquettent pas en série | Codes archivés |
| Calculateurs | Hors du besoin | — (aucune donnée) |
| Quatre rôles et la grille de droits | Deux rôles suffisent | Ancien rôle de chacun archivé |
| Notifications, recherche globale, traduction arabe | Hors du besoin ; interface entièrement en français | — |

« Archivées » : déplacées telles quelles dans le schéma `archives` de la même
base, consultables à tout moment (voir [MISE-A-JOUR.md](MISE-A-JOUR.md)).

## 3. Ce qui a été simplifié

| Avant | Maintenant |
|-------|------------|
| 19 entrées de menu | 7 sections : Tableau de bord, Produits, Commandes, Clients, Ventes, Rapports, Paramètres |
| 49 tables | 19 tables, clés étrangères explicites, suppression refusée tant qu'une fiche est utilisée |
| 4 rôles, droits détaillés | Employé et Administrateur ; seuls les comptes et les sauvegardes sont réservés à l'administrateur |
| Commande en dix statuts | Reçue → En fabrication → Prête → Livrée (ou Annulée) |
| 12 rapports, dont la moitié sur la production | 8 rapports : ventes par jour, ventes par mois, bénéfices, dépenses, dettes clients, produits les plus vendus, les plus rentables, état du stock |
| Facture puis paiement sur facture | Paiement directement sur la vente ou la commande ; reste à payer calculé |
| Tableau de bord d'attente | Tableau de bord réel : jour, mois, commandes, stock, graphiques |
| Une liste d'écrans écrits un par un | Un seul modèle de liste (colonnes, filtres, actions) et une seule fenêtre de saisie |

## 4. Ce qui a été conservé (et complété)

* **Architecture** : Domaine, Application, Infrastructure, Présentation,
  application WPF ; EF Core et PostgreSQL ; QuestPDF, ClosedXML, Serilog.
* **Données** : clients, produits, photos, commandes, ventes, paiements,
  dépenses, mouvements de stock, comptes et mots de passe, journal d'audit,
  numérotation.
* **Sécurité** : hachage PBKDF2, blocage après cinq essais, journal d'audit,
  changement de mot de passe proposé, porte de secours de l'administrateur.
* **Sauvegarde et restauration** en ZIP lisible (CSV), désormais avec les
  photos ; restauration des sauvegardes de l'ancienne version.
* **Ajouté** pour les besoins exprimés :
  * pièces uniques (stock 0 ou 1, jamais vendues deux fois, pas d'alerte de
    rupture une fois vendues) ;
  * canal de la commande (atelier, Instagram, WhatsApp, Facebook, téléphone)
    et compte Instagram du client ;
  * galerie de photos des produits et des commandes ;
  * fiche client complète (achats, commandes, paiements, reste dû) ;
  * entrées en stock, casses avec motif, corrections après comptage ;
  * encaissement du reste d'une vente à crédit, acomptes successifs ;
  * annulation avec remboursement d'une vente payée ou d'une commande avec
    acompte ;
  * chiffre d'affaires comptant les commandes livrées, pas seulement les
    ventes au comptoir ;
  * réglage de la sauvegarde quotidienne depuis le logiciel.

## 5. Défauts corrigés

| Défaut de l'application d'origine | Correction |
|-----------------------------------|------------|
| Une vente s'écrivait en plusieurs fois, sans transaction : un produit manquant en stock pouvait laisser une vente sans sortie de stock, ou un stock diminué sans vente | Vente, lignes et sorties de stock en une seule écriture, règlement dans la même transaction ; idem pour les annulations, les paiements et les mouvements manuels. Vérifié sur un vrai PostgreSQL |
| Dates enregistrées en UTC affichées telles quelles : reçus décalés d'une heure, date choisie décalée d'un jour, commande « en retard » dès 1 h du matin | Toutes les dates sont converties à l'heure d'Alger, les échéances comparées par jour d'atelier |
| Chiffre d'affaires ignorant les commandes livrées | Ventes + commandes livrées, dans le tableau de bord et les rapports |
| Tableau de bord affichant un message d'attente | Tableau de bord réel |
| Sélecteur de rapport inopérant (lié à un tuple) | Liste de rapports liée à des objets |
| « Modifier » une dépense en créait une seconde | Modification réelle de la dépense |
| Filtres des listes déclarés mais jamais affichés | Filtres visibles et actifs |
| Actions de l'écran Utilisateurs cachées | Désactiver, réactiver, nouveau mot de passe accessibles |
| Sauvegardes écrites dans `Program Files`, en lecture seule | `%LOCALAPPDATA%\CeramiPro\sauvegardes` ; les anciennes sont recopiées |
| Sauvegarde automatique désactivée et impossible à activer depuis le logiciel | Activée par défaut à 13 h, réglable dans *Paramètres → Sauvegarde* |
| Règle du mot de passe affichée mais jamais appliquée | Appliquée à la création, à la réinitialisation et au changement |
| Annulation d'un paiement sans motif conservé | Motif obligatoire, conservé |
| Échec de mise à jour de la base annoncé comme « base injoignable » | Message distinct, détail dans le journal |
| Erreurs inattendues non journalisées | Chaque erreur est écrite dans le journal |
| Produit ou client sans nom acceptés | Refusés par le service |
| Droits vérifiés seulement par le menu pour les comptes et les sauvegardes | Vérifiés aussi par les services |
| Deux postes modifiant la même fiche : la dernière écriture écrasait l'autre | Jeton de version : l'enregistrement est refusé avec un message clair |

## 6. Vérifications

* **318 tests automatiques** (xUnit), tous au vert :
  * règles métier : stock, pièces uniques, ventes, remises, crédits,
    acomptes, livraisons, annulations et remboursements, rapports, dates
    d'Alger ;
  * écrans pilotés comme au clic (vrais services, fenêtres simulées) :
    caisse, commandes, stock, ventes, dépenses, exports, menu, connexion ;
  * cohérence des écrans : chaque colonne lit une propriété qui existe, chaque
    écran a sa vue, chaque formulaire est enregistré ;
  * sur un vrai PostgreSQL : installation neuve (19 tables), mise à jour d'une
    base de l'ancienne version, restauration d'une ancienne sauvegarde,
    ouverture de chaque écran, annulation complète d'une vente dont le
    règlement échoue.
* **Couche WPF** compilée contre les assemblages de référence de Windows et
  XAML vérifiés (`outils/verifier-application-windows.sh`).

L'exécutable final (`CeramiPro.exe`, `CeramiPro_Setup.exe`) se construit sous
Windows avec `deploiement\publier.bat` puis
`deploiement\construire-installateur.bat`.
