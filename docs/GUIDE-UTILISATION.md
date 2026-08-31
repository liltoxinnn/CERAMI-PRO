# Guide d'utilisation de CERAMIPRO

Ce guide s'adresse à la personne qui fait tourner l'atelier au quotidien.
Il suit l'ordre dans lequel on utilise le logiciel une journée type.

---

## Se connecter

1. Ouvrez l'adresse du logiciel dans le navigateur.
2. Saisissez votre nom d'utilisateur et votre mot de passe.
3. À la première connexion, le logiciel demande de choisir un nouveau mot de passe.

Chaque personne doit avoir son propre compte : le logiciel note qui a fait quoi.

---

## L'écran d'accueil

Le tableau de bord répond à trois questions :

* **Aujourd'hui** — combien a-t-on vendu, encaissé, et quel bénéfice ?
* **Ce mois-ci** — chiffre d'affaires, dépenses, résultat.
* **Que faut-il surveiller ?** — productions en cours, commandes proches de
  l'échéance, stocks au minimum, argent restant à encaisser.

Les graphiques montrent l'évolution des ventes sur trente jours et sur douze
mois, ainsi que les produits les plus vendus et les plus rentables.

---

## Trouver une fiche

Trois moyens, selon ce que vous avez sous la main :

| Vous avez… | Utilisez |
|------------|----------|
| Un nom, même mal orthographié | Le champ de recherche en haut de l'écran |
| Une étiquette avec un code | L'écran « Scanner un code » |
| Rien de précis | Le menu de gauche |

La recherche tolère les fautes de frappe et les accents manquants :
« emaille » retrouve « Émaillé », « benalli » retrouve « Benali ».

---

## Les alertes

La cloche en haut de l'écran indique ce qui demande votre attention :

* un produit ou une matière au niveau minimum ;
* une commande dont la date approche ou est dépassée ;
* une production en retard ou qui n'avance plus ;
* un client ou un fournisseur avec un reste à payer.

Les alertes se recalculent toutes seules : quand vous réapprovisionnez un
stock, l'alerte disparaît sans qu'il y ait rien à faire.

L'écran « Réglages des alertes » permet de choisir celles que vous voulez voir
et à partir de quel moment elles apparaissent.

---

## Une journée type

### Le matin — préparer la production

1. **Ordres de production** → « Nouvel ordre » : choisissez le produit et la
   quantité. Le logiciel vérifie les matières disponibles et refuse de lancer
   une production impossible, en indiquant précisément ce qui manque.
2. Les matières sont retirées du stock au lancement, jamais avant.
3. Suivez l'avancement dans le **Tableau de production** : chaque pièce passe
   par le façonnage, le séchage, la première cuisson, l'émaillage, la cuisson
   finale, la décoration puis le contrôle qualité.

Une production ne peut pas être marquée « Terminée » sans contrôle qualité.

### En journée — vendre

1. **Nouvelle vente** : choisissez le client (ou laissez « client de passage »),
   puis ajoutez les produits. Une douchette USB ou la caméra du téléphone
   ajoute directement le produit scanné.
2. Le logiciel refuse de vendre plus que le stock disponible.
3. Encaissez tout ou partie : le reste devient une dette du client, suivie dans
   « Dettes clients ».
4. La facture est créée automatiquement ; le bouton « Imprimer » permet de
   l'imprimer ou de l'enregistrer en PDF.

### Le soir — vérifier

* **Rapports** : chiffre d'affaires, bénéfices, dépenses, dettes, consommation
  des matières, produits les plus vendus… Chaque rapport s'imprime ou s'exporte
  vers un tableur.
* **Alertes** : ce qu'il faudra traiter demain.

---

## Les étiquettes

L'écran **Étiquettes** prépare une planche à imprimer : nom du produit, prix,
code QR et code-barres. Collez l'étiquette sur la pièce ; il suffira ensuite de
la scanner pour retrouver sa fiche ou l'ajouter à une vente.

Les codes-barres sont au format Code 39, reconnu par les douchettes USB
courantes. La lecture par la caméra fonctionne sur les téléphones récents.

---

## Les calculateurs

* **Matières** — combien d'argile et d'émail pour fabriquer *n* pièces, et le
  coût correspondant, en comparant avec le stock réel.
* **Surface** — surface à couvrir, perte comprise.
* **Quantité** — nombre de sacs, plaques ou emballages à prévoir.

---

## Ce que le logiciel ne laisse pas faire

Ces refus protègent la cohérence des chiffres :

* vendre ou consommer plus que le stock disponible ;
* lancer une production sans les matières nécessaires ;
* terminer une production sans contrôle qualité ;
* livrer une commande qui n'est pas entièrement payée ;
* revenir en arrière dans les étapes de production ou de commande ;
* supprimer définitivement une vente, une facture, un paiement ou une dépense
  (ces pièces sont conservées, marquées comme supprimées, avec le motif).

L'administrateur peut lever certains de ces blocages : chaque dérogation est
enregistrée avec son motif et le nom de la personne.

---

## Sauvegarder

L'écran **Sauvegardes** crée une archive contenant toutes les données de
l'atelier. Copiez-la régulièrement sur une clé USB ou un disque externe.

La sauvegarde automatique quotidienne s'active dans « Paramètres », rubrique
Sauvegarde : indiquez l'heure et le nombre de jours de conservation.
