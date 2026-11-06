# CeramiPro — gestion d'un atelier de céramique

Application **Windows** (WPF, .NET) pour un atelier de céramique artisanale :
produits et stock, commandes personnalisées reçues sur Instagram ou WhatsApp,
clients, ventes avec reçu, dépenses et rapports. Interface entièrement en
français, configurée pour l'Algérie : dinar (`45 000,00 DA`), dates
`31/08/2026`, heure d'Alger.

```
Bureau  →  CeramiPro.exe  →  Fenêtre Windows  →  PostgreSQL (sur le poste)
```

Pas de navigateur, pas de site web, pas d'adresse `localhost`.

---

## Les sept sections

| Section | Ce qu'on y fait |
|---------|-----------------|
| **Tableau de bord** | Ventes et encaissements du jour, chiffre d'affaires et bénéfice du mois, commandes en retard ou prêtes, produits à refabriquer, graphiques des ventes |
| **Produits** | Catalogue avec photos, prix, coût, description ; pièces uniques ; stock, entrées, casses, corrections après comptage ; historique de chaque mouvement ; catégories |
| **Commandes** | Commandes sur mesure (atelier, Instagram, WhatsApp, Facebook, téléphone), acomptes, reste à payer calculé, étapes Reçue → En fabrication → Prête → Livrée, photos du modèle, reçu |
| **Clients** | Fiche (téléphone, Instagram, adresse), historique des achats, des commandes et des paiements, reste dû |
| **Ventes** | Nouvelle vente (caisse), historique, encaissement du reste d'une vente à crédit, annulation avec retour en stock, paiements, reçu 80 mm |
| **Rapports** | Ventes par jour et par mois, bénéfices, dépenses, dettes clients, produits les plus vendus et les plus rentables, état du stock ; export Excel et PDF ; saisie des dépenses |
| **Paramètres** | Identité et mentions légales de l'atelier, monnaie, numérotation, état de la base ; catégories de dépenses ; comptes utilisateurs et sauvegarde (administrateur) |

Le stock se met à jour tout seul à chaque vente et à chaque annulation. Une
vente, son règlement et ses sorties de stock s'enregistrent ensemble ou pas du
tout. Rien n'est effacé : une vente ou un paiement annulé reste dans
l'historique, avec son motif.

---

## Deux rôles

| Rôle | Peut |
|------|------|
| **Employé** | Tout le travail quotidien : produits, stock, commandes, clients, ventes, dépenses, rapports |
| **Administrateur** | En plus : créer et gérer les comptes, sauvegarder et restaurer la base |

Le menu cache ce qui n'est pas permis, et les services le refusent aussi.
Mots de passe hachés (PBKDF2), au moins huit caractères avec majuscule,
minuscule et chiffre ; cinq essais manqués bloquent le compte un quart d'heure.

---

## Installer sur l'ordinateur de l'atelier

1. **PostgreSQL** (14 ou plus récent) :
   ```bat
   winget install PostgreSQL.PostgreSQL.16
   ```
   L'installateur demande un mot de passe pour l'utilisateur `postgres` : notez-le.

2. **CeramiPro** : lancez `CeramiPro_Setup.exe`. Il installe le logiciel dans
   `C:\Program Files\CeramiPro`, crée les raccourcis, et propose d'ouvrir le
   fichier de réglages.

3. Dans `%LOCALAPPDATA%\CeramiPro\appsettings.Local.json`, renseignez le mot
   de passe PostgreSQL après `Password=`, puis enregistrez.

4. Lancez CeramiPro. Au premier démarrage, la base `CeramiProDB` est créée.
   Connectez-vous avec `admin` / `CeramiPro@2026` ; le logiciel propose de
   changer ce mot de passe — faites-le.

L'exécutable est autonome : il n'y a pas de .NET à installer sur le poste.

### Où vivent les fichiers

| Emplacement | Contenu |
|-------------|---------|
| `C:\Program Files\CeramiPro` | Le programme (lecture seule) |
| `%LOCALAPPDATA%\CeramiPro\appsettings.Local.json` | Mot de passe de la base (jamais livré, jamais versionné) |
| `%LOCALAPPDATA%\CeramiPro\photos` | Photos des produits et des commandes |
| `%LOCALAPPDATA%\CeramiPro\sauvegardes` | Archives de sauvegarde |
| `%LOCALAPPDATA%\CeramiPro\journaux` | Journal du logiciel, un fichier par jour |

---

## Mettre à jour une installation existante

**Sauvegardez d'abord** (Paramètres → Sauvegarde → *Sauvegarder maintenant*,
puis copiez l'archive sur une clé USB). Installez ensuite le nouveau
`CeramiPro_Setup.exe` par-dessus l'ancien : au premier démarrage, la base est
mise à jour automatiquement, sans perte de données.

Le détail — ce qui change dans la base, où sont rangées les anciennes données,
comment revenir en arrière — est dans [docs/MISE-A-JOUR.md](docs/MISE-A-JOUR.md).

---

## Sauvegarde

**Paramètres → Sauvegarde.** Une archive ZIP contient chaque table au format
CSV (lisible sans le logiciel) et les photos. La sauvegarde automatique est
active chaque jour à partir de 13 h quand le logiciel est ouvert ; l'heure et
la durée de conservation se règlent sur le même écran.

Copiez régulièrement la dernière archive sur une clé USB : une sauvegarde
restée sur le même ordinateur ne protège de rien.

**Restaurer** remplace toutes les données actuelles par celles de l'archive :
deux confirmations sont demandées ; sauvegardez l'état présent avant. Les
archives produites par l'ancienne version du logiciel se restaurent aussi.

---

## Mot de passe administrateur oublié

Ajoutez dans `appsettings.Local.json` :

```json
{
  "Administrateur": {
    "ReinitialiserMotDePasse": "true",
    "MotDePasseInitial": "UnNouveauMotDePasse@2026"
  }
}
```

Lancez le logiciel une fois : le compte `admin` reprend ce mot de passe et
est débloqué. **Retirez ensuite ces lignes** : tant qu'elles restent, chaque
démarrage réinitialise le compte.

---

## Pour les développeurs

### Construire l'exécutable et l'installateur (sous Windows)

```bat
winget install Microsoft.DotNet.SDK.8
winget install JRSoftware.InnoSetup
deploiement\publier.bat
deploiement\construire-installateur.bat
```

Le premier script produit `deploiement\publication\CeramiPro.exe` (autonome,
win-x64) ; le second `deploiement\CeramiPro_Setup.exe`.

Sans poste Windows de développement : sur GitHub, onglet **Actions →
Exécutable Windows → Run workflow**. Une machine Windows de GitHub lance les
tests, compile l'application, et met `CeramiPro_Setup.exe` à télécharger en
bas de la page de l'exécution.

### Lancer depuis les sources

```bat
copy src\CeramiPro.App\appsettings.Local.exemple.json src\CeramiPro.App\appsettings.Local.json
notepad src\CeramiPro.App\appsettings.Local.json
dotnet run --project src\CeramiPro.App
```

### Solutions et tests

| Fichier | Contenu | Système |
|---------|---------|---------|
| `CeramiPro.sln` | Domaine, application, infrastructure, présentation, tests | Windows, Linux, macOS |
| `CeramiPro.Windows.sln` | Tout, y compris l'application WPF | Windows |

```bat
dotnet test CeramiPro.sln
python outils\verifier-xaml.py
```

318 tests : règles métier, écrans pilotés comme au clic, et, si un PostgreSQL
répond (`CERAMIPRO_TEST_DB` pour en viser un autre), migration d'une base de
l'ancienne version, installation neuve, restauration et transactions réelles.
Sans serveur, ces derniers sont ignorés plutôt que déclarés en échec.

Hors de Windows, `outils/verifier-application-windows.sh` compile la couche
WPF contre les assemblages de référence de Windows et vérifie les XAML.

### Version de .NET

Le projet cible .NET 8 ; pour passer à .NET 10, changez les deux lignes de
`Directory.Build.props` (`net10.0`, `net10.0-windows`), rien d'autre.

---

## Documentation

| Document | Contenu |
|----------|---------|
| [docs/REMANIEMENT.md](docs/REMANIEMENT.md) | Audit, ce qui a été retiré, simplifié, conservé, et les corrections |
| [docs/MISE-A-JOUR.md](docs/MISE-A-JOUR.md) | Mise à jour d'une base existante, archives, retour en arrière |
| [docs/BASE-DE-DONNEES.md](docs/BASE-DE-DONNEES.md) | Les 19 tables et leurs relations |
| [docs/NAVIGATION.md](docs/NAVIGATION.md) | Menu et fonctionnement des écrans |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Couches, règles, journalisation |
