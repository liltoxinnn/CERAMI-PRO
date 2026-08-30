# Règles métier

Les vingt règles demandées et l'endroit exact où elles sont appliquées.
« Étape n » renvoie au plan de développement.

## Stock

| N° | Règle | Mise en œuvre |
|----|-------|---------------|
| 1 | Ne jamais permettre un stock négatif par défaut | Réglage `stock.autoriser.negatif` créé à l'installation (valeur `false`) ; contrôle appliqué à l'étape 2 dans le service de mouvements |
| 2 | Chaque mouvement de stock est enregistré | Table `InventoryTransactions` : type explicite, stock avant, stock après, document d'origine, auteur. Aucun écran ne modifie une quantité sans passer par elle |
| 3 | Une vente diminue automatiquement le stock | Service de vente, étape 7 |
| 4 | Une production terminée augmente le stock des produits finis | Service de production, étape 4 |
| 5 | Une production consomme les matières premières | `ProductionMaterials` + mouvement de type « Consommation en production », étape 4 |
| 6 | Une annulation inverse correctement les mouvements | Champ `ReversedTransactionId` : le mouvement d'annulation cite le mouvement annulé |

## Production

| N° | Règle | Mise en œuvre |
|----|-------|---------------|
| 7 | Vérifier les matières disponibles avant une production | Contrôle de disponibilité au lancement, message indiquant la matière manquante, étape 4 |
| 8 | Enregistrer les quantités prévues, produites et endommagées | `ProductionOrders.PlannedQuantity`, `CompletedQuantity`, `DamagedQuantity` |
| 9 | Les pièces endommagées sont enregistrées séparément | `DamagedQuantity` sur l'ordre, l'étape, la fournée et le contrôle qualité |
| 10 | Le produit ne devient « Terminé » qu'après le contrôle qualité | Transition d'état conditionnée par un `QualityCheck` conforme, étape 5 |
| 11 | Un administrateur peut accorder une dérogation | `StockCheckOverridden`, `OverriddenByUserId`, `OverrideReason` ; droit `production.deroger` réservé à l'administrateur |

## Paiements

| N° | Règle | Mise en œuvre |
|----|-------|---------------|
| 12 | Les paiements partiels sont supportés | `Payments` indépendants de la vente ; `PaidAmount` cumulé sur le document |
| 13 | Le reste à payer est calculé automatiquement | Propriétés calculées `RemainingAmount` sur `Sale`, `Invoice`, `CustomOrder`, `Purchase` |
| 14 | Chaque paiement est enregistré individuellement | Une ligne par règlement, avec numéro, mode, référence, auteur et date |
| 15 | Les transactions financières ne sont jamais supprimées définitivement | `ISoftDeletable` sur `Sale`, `Invoice`, `Payment`, `SupplierPayment`, `Purchase`, `Expense`, `CustomOrder`, `ProductionOrder` ; la suppression devient un marquage, appliqué automatiquement par le contexte de données |

## Commandes

| N° | Règle | Mise en œuvre |
|----|-------|---------------|
| 16 | Une commande personnalisée a une date limite | `CustomOrders.Deadline`, obligatoire et indexée |
| 17 | Les commandes en retard sont signalées | Alertes « Commande proche de l'échéance » (3 jours) et « Commande en retard », créées à l'installation |
| 18 | Les acomptes sont enregistrés comme paiements | `Payments.IsDeposit` |
| 19 | Une commande conserve ses photos et ses notes | `CustomOrderImages` (référence, croquis, fabrication, pièce terminée) et `CustomOrderNotes` |

## Audit

| N° | Règle | Mise en œuvre |
|----|-------|---------------|
| 20 | Les opérations importantes sont journalisées | `IAuditService` → table `AuditLogs` : auteur, action, entité, description, adresse réseau, date. Déjà actif sur les connexions, les échecs de connexion, la création et la modification d'utilisateurs, la modification des droits et des paramètres |

---

## Règles complémentaires appliquées dès l'étape 1

| Règle | Raison |
|-------|--------|
| Le logiciel conserve toujours au moins un administrateur actif | Éviter de rendre le logiciel inutilisable |
| Un utilisateur ne peut pas désactiver son propre compte | Éviter de se déconnecter définitivement par erreur |
| Le rôle « Administrateur » ne peut pas être restreint | Garantir qu'un accès complet subsiste |
| Cinq échecs de connexion bloquent le compte quinze minutes | Freiner les tentatives de découverte de mot de passe |
| Le message d'erreur de connexion est identique pour un compte inconnu et un mauvais mot de passe | Ne pas révéler quels comptes existent |
| Désactiver un compte invalide immédiatement sa session | Une exclusion doit prendre effet tout de suite |
| Changer son mot de passe invalide les sessions ouvertes | Reprendre la main après un mot de passe compromis |
| Les lignes d'un document supprimé logiquement disparaissent des listes et des rapports | Une vente annulée ne doit plus alimenter les statistiques |

---

## Où lire le code correspondant

| Sujet | Fichier |
|-------|---------|
| Suppression logique et traçabilité automatique | `src/CeramicWorkshop.Infrastructure/Data/ApplicationDbContext.cs` |
| Blocage de compte et connexion | `src/CeramicWorkshop.Application/Services/AuthService.cs` |
| Dernier administrateur, auto-désactivation | `src/CeramicWorkshop.Application/Services/UtilisateurService.cs` |
| Protection du rôle administrateur | `src/CeramicWorkshop.Application/Services/RoleService.cs` |
| Journal des opérations | `src/CeramicWorkshop.Infrastructure/Services/AuditService.cs` |
| Catalogue des droits par rôle | `src/CeramicWorkshop.Domain/Common/PermissionCodes.cs` |

Chacune de ces règles est couverte par au moins un test dans `tests/UnitTests`.
