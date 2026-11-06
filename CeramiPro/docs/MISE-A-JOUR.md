# Mettre à jour une installation existante

Cette version remplace l'ancienne base de 49 tables par 19 tables. La mise à
jour est **automatique** et **ne supprime aucune donnée saisie par
l'atelier** : ce qui reste utile est converti sur place, ce qui appartient aux
modules retirés est rangé dans un schéma `archives` de la même base.

## Marche à suivre

1. **Sauvegardez** avec l'ancienne version : *Administration → Sauvegarde →
   Sauvegarder maintenant*. Copiez l'archive sur une clé USB.
2. Fermez CeramiPro sur **tous** les postes.
3. Lancez le nouveau `CeramiPro_Setup.exe` : il s'installe par-dessus
   l'ancien (même dossier, mêmes raccourcis). `appsettings.Local.json`, les
   photos et les sauvegardes ne sont pas touchés.
4. Ouvrez CeramiPro sur **un seul** poste et attendez l'écran de connexion :
   la base est mise à jour pendant ce premier démarrage.
5. Connectez-vous et vérifiez quelques fiches (un client, une commande en
   cours, le stock d'un produit). Ouvrez ensuite les autres postes.

Si la mise à jour échoue (base injoignable, disque plein…), elle est
**entièrement annulée** — chaque migration s'exécute dans une transaction — et
le logiciel affiche le motif : la base reste dans l'état de l'ancienne
version. Le détail est dans `%LOCALAPPDATA%\CeramiPro\journaux`.

## Ce que la mise à jour fait aux données

| Ancienne donnée | Devient |
|-----------------|---------|
| Clients, notes clients | inchangés |
| Produits, catégories, photos | inchangés ; l'image principale reste principale |
| Variantes de produits | leur stock s'ajoute à celui du produit ; le nom de la variante est noté sur les mouvements concernés ; la table est archivée |
| Codes-barres et QR codes | archivés (`archives."Products_avant_simplification"`) |
| Ventes, lignes de vente | inchangées |
| Factures | archivées ; les paiements qui pointaient vers une facture sont rattachés à sa vente ou à sa commande |
| Paiements clients | inchangés |
| Règlements aux fournisseurs | marqués annulés (motif : *module fournisseurs retiré*) et archivés, pour ne pas fausser les encaissements |
| Commandes personnalisées | inchangées ; les dix étapes deviennent quatre : *conception* et *validation* → **Reçue** ; *cuisson*, *décoration* et *contrôle* → **En fabrication** ; l'ancienne étape est notée dans la commande |
| Mouvements de stock des produits | conservés ; les anciens types *entrée de production*, *consommation*, *transfert* deviennent **Entrée** ou **Régularisation**, avec l'ancien type noté |
| Mouvements des matières premières | retirés de l'historique courant, gardés en entier dans `archives."InventoryTransactions_avant_simplification"` |
| Dépenses | inchangées |
| Comptes utilisateurs | conservés avec leur mot de passe ; l'ancien rôle *administrateur* devient **Administrateur**, les autres **Employé** ; s'il n'y avait aucun administrateur actif, le compte `admin` (ou le premier compte actif) le devient. L'ancien rôle de chacun est archivé |
| Matières premières, fournisseurs, achats, recettes, production, fours, cuissons, décoration, contrôles qualité, unités, ajustements de stock | tables déplacées telles quelles dans `archives` si elles contiennent des données, supprimées si elles sont vides |
| Rôles, droits, notifications | supprimés : ce sont des réglages du logiciel, pas des données de l'atelier |
| Préfixes de numérotation des documents retirés | archivés (`archives."BusinessSettings_avant_simplification"`) |

Les numéros déjà attribués (clients, produits, ventes, commandes, paiements,
dépenses) ne changent pas, et la numérotation continue à la suite.

## Consulter les archives

Le schéma `archives` est en lecture seule de fait : le logiciel ne le lit ni
ne l'écrit, et ses liens vers les tables vivantes ont été retirés pour qu'il
n'empêche rien. Il se consulte avec pgAdmin (installé avec PostgreSQL) :

```sql
SELECT table_name FROM information_schema.tables WHERE table_schema = 'archives';
SELECT * FROM archives."Materials";
SELECT * FROM archives."InventoryTransactions_avant_simplification";
```

L'écran *Paramètres → Atelier* indique combien de tables archivées la base
contient.

## Revenir en arrière

La migration ne se défait pas automatiquement (elle refuse de le faire, plutôt
que de recréer des tables vides). Pour revenir à l'ancienne version :

1. réinstallez l'ancien `CeramiPro_Setup.exe` ;
2. supprimez la base `CeramiProDB` dans pgAdmin ;
3. démarrez l'ancienne version, qui recrée une base vide ;
4. restaurez la sauvegarde faite à l'étape 1 de la mise à jour.

## Anciennes sauvegardes

Les archives ZIP produites par l'ancienne version se restaurent dans la
nouvelle (*Paramètres → Sauvegarde → Restaurer*) : elles subissent les mêmes
conversions que la base. Celles qui étaient rangées dans le dossier du
programme sont recopiées, au premier démarrage, dans
`%LOCALAPPDATA%\CeramiPro\sauvegardes`.

## Vérifié par les tests

`tests/CeramiPro.Tests/MonteeDeVersionTests.cs` charge dans un vrai PostgreSQL
une base de l'ancienne version (comptes, produits à variantes, vente facturée
et payée, commandes avec acompte, matières, achats, mouvements), applique la
mise à jour, puis vérifie chaque conversion ci-dessus, l'absence de perte, le
fonctionnement normal du logiciel ensuite, et que la restauration d'une
ancienne sauvegarde donne le même résultat.
