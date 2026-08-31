# CeramiPro — Logiciel de gestion d'atelier de céramique

Application **Windows native** pour un atelier de céramique et de décoration :
matières premières, production, séchage, cuisson, décoration, contrôle qualité,
produits finis, commandes sur mesure, ventes, paiements et facturation.

Interface entièrement en français, avec l'arabe en seconde langue. Configurée
pour l'Algérie : dinar algérien (`45 000,00 DA`), dates `31/08/2026`, fuseau
`Africa/Algiers`.

```
Bureau  →  CeramiPro.exe  →  Fenêtre Windows
```

Pas de navigateur, pas de serveur web, pas d'adresse `localhost`.

---

## Ce que le logiciel sait faire

| Module | Contenu |
|--------|---------|
| **Caisse** | Vente au comptoir, douchette, rendu de monnaie, reçu imprimé |
| **Stock** | Matières, produits finis, mouvements, alertes, valeur immobilisée |
| **Produits** | Catalogue, catégories, recettes de fabrication, étiquettes |
| **Production** | Ordres, tableau par étape, lancement des matières, historique |
| **Cuisson** | Fours, enfournement, défournement, coût d'énergie réparti |
| **Décoration** | Travaux, types, suivi |
| **Qualité** | Contrôles avant emballage, pièces acceptées, refusées, à retoucher |
| **Clients** | Fiches, historique, commandes sur mesure, dettes |
| **Fournisseurs** | Fiches, achats, réceptions, règlements |
| **Finances** | Ventes, factures, paiements, dépenses, douze rapports |
| **Administration** | Comptes, rôles et droits, unités, catégories, sauvegarde |

Chaque liste s'exporte vers Excel ou s'imprime en PDF. Les factures, les reçus
de caisse et les planches d'étiquettes sont produits au format PDF.

---

## Sécurité

Quatre rôles : **Administrateur**, **Responsable**, **Employé**, **Caissier**.

Les droits sont vérifiés dans la couche métier, et pas seulement dans les
écrans : masquer un bouton n'empêche rien, refuser l'opération si.

Les mots de passe sont hachés (PBKDF2 HMAC-SHA512, 210 000 itérations). Un
compte créé par un administrateur reçoit un mot de passe provisoire, et le
logiciel propose de le remplacer à chaque connexion tant qu'il reste en usage.
La proposition n'empêche jamais d'ouvrir l'atelier : le changement se fait
aussi à tout moment depuis le bas du menu. Cinq tentatives manquées bloquent
le compte un quart d'heure.

Toute opération sensible est inscrite au journal d'audit.

---

## Prérequis

* [.NET SDK 8](https://dotnet.microsoft.com/download) ou supérieur
* [PostgreSQL 14](https://www.postgresql.org/download/) ou supérieur
* Windows 10 ou 11

```bat
winget install Microsoft.DotNet.SDK.8
winget install PostgreSQL.PostgreSQL.16
```

Fermez puis rouvrez le terminal. L'installateur PostgreSQL demande un mot de
passe pour l'utilisateur `postgres` : notez-le.

---

## Lancer l'application

```bat
copy src\CeramiPro.App\appsettings.Local.exemple.json src\CeramiPro.App\appsettings.Local.json
notepad src\CeramiPro.App\appsettings.Local.json
```

Renseignez le mot de passe PostgreSQL après `Password=`, enregistrez, puis :

```bat
dotnet run --project src\CeramiPro.App
```

La base `CeramiProDB` et ses tables sont créées au premier démarrage.

Le compte de départ est `admin`, mot de passe `CeramiPro@2026` ; le logiciel
propose d'en choisir un autre à la première connexion, sans l'imposer.

---

## Installer sur l'ordinateur de l'atelier

```bat
deploiement\publier.bat
deploiement\construire-installateur.bat
```

Le premier script produit `deploiement\publication\CeramiPro.exe`, autonome :
l'atelier n'a pas à installer .NET. Le second construit
`deploiement\CeramiPro_Setup.exe`, qui pose le logiciel, crée les raccourcis et
ouvre le fichier de réglages pour y saisir le mot de passe de la base.

Le second demande [Inno Setup 6](https://jrsoftware.org/isinfo.php), gratuit :

```bat
winget install JRSoftware.InnoSetup
```

PostgreSQL reste à installer séparément sur le poste : c'est un service, et son
mot de passe appartient à l'atelier.

### Où vivent les fichiers

| Emplacement | Contenu |
|-------------|---------|
| `C:\Program Files\CeramiPro` | Le programme |
| `%LOCALAPPDATA%\CeramiPro` | Réglages locaux, journaux, sauvegardes, documents |

Le fichier `appsettings.Local.json` est cherché aux deux endroits, celui du
dossier de données l'emportant : il se modifie ainsi sans droits
d'administrateur.

---

## Mot de passe administrateur oublié

Les mots de passe sont hachés : ils ne peuvent pas être retrouvés, même en
lisant la base. Une porte de secours existe, qui exige d'écrire dans un
fichier de l'ordinateur de l'atelier.

Ajoutez ces deux lignes dans `appsettings.Local.json` :

```json
{
  "Administrateur": {
    "ReinitialiserMotDePasse": "true",
    "MotDePasseInitial": "UnNouveauMotDePasse@2026"
  }
}
```

Lancez le logiciel une fois : le compte `admin` retrouve ce mot de passe, et
un compte bloqué par des tentatives répétées est débloqué.

**Retirez ensuite ces deux lignes**, puis changez le mot de passe depuis le
logiciel. Tant qu'elles restent, chaque démarrage réinitialise le compte.

---

## Sauvegarde

L'écran **Administration → Sauvegarde** produit une archive ZIP contenant une
copie lisible de chaque table, au format CSV : elle se relit sans le logiciel,
ce qui est la première qualité d'une sauvegarde. Une sauvegarde automatique
tourne chaque jour, avec purge des archives trop anciennes.

Copiez régulièrement la dernière archive sur une clé USB : une sauvegarde
restée sur le même ordinateur ne protège de rien.

Le bouton **Restaurer** du même écran remet la base dans l'état d'une archive
choisie. L'opération remplace toutes les données actuelles et ne se défait
pas : elle demande deux confirmations, et il faut sauvegarder l'état présent
avant de s'en servir. Fermez puis rouvrez le logiciel après une restauration.

---

## Documentation

| Document | Contenu |
|----------|---------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Couches, dépendances, règles |
| [docs/BASE-DE-DONNEES.md](docs/BASE-DE-DONNEES.md) | Arbre des 49 tables et relations |
| [docs/NAVIGATION.md](docs/NAVIGATION.md) | Menu et fonctionnement des écrans |
| [docs/PLAN-DE-DEVELOPPEMENT.md](docs/PLAN-DE-DEVELOPPEMENT.md) | Les 13 étapes |

---

## Deux solutions

| Fichier | Contenu | Système |
|---------|---------|---------|
| `CeramiPro.sln` | Domaine, application, infrastructure, présentation, tests | Windows, Linux, macOS |
| `CeramiPro.Windows.sln` | Tout, y compris l'application WPF | Windows uniquement |

WPF ne se compile que sous Windows. Séparer les deux solutions permet de
compiler et de tester toute la logique métier sur n'importe quelle machine,
et de réserver à Windows ce qui lui est propre.

---

## Tests

```bat
dotnet test CeramiPro.sln
python outils\verifier-xaml.py
```

Les tests qui ont besoin de PostgreSQL sont ignorés si aucun serveur ne
répond, plutôt que déclarés en échec. Pour viser un autre serveur, définissez
la variable `CERAMIPRO_TEST_DB`.

Le vérificateur XAML remplace le compilateur WPF, indisponible hors de
Windows : il contrôle les classes compagnons, les ressources employées, les
gestionnaires d'événements et les commandes liées.

---

## Version de .NET

Le cahier des charges vise .NET 10. Le développement a été mené sur .NET 8,
seule version disponible sur la machine de compilation. Pour basculer, changez
les deux lignes de `Directory.Build.props` :

```xml
<VersionDotNet>net10.0</VersionDotNet>
<VersionDotNetWindows>net10.0-windows</VersionDotNetWindows>
```

Aucun autre fichier n'est à modifier.
