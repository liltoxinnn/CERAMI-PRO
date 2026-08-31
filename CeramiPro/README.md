# CeramiPro — Logiciel de gestion d'atelier de céramique

Application **Windows native** pour un atelier de céramique et de décoration :
matières premières, production, séchage, cuisson, décoration, contrôle qualité,
produits finis, commandes sur mesure, ventes, paiements et facturation.

Interface entièrement en français, configurée pour l'Algérie : dinar algérien
(`45 000,00 DA`), dates `31/08/2026`, fuseau `Africa/Algiers`.

```
Bureau  →  CeramiPro.exe  →  Fenêtre Windows
```

Pas de navigateur, pas de serveur web, pas d'adresse `localhost`.

---

## État d'avancement

| Étape | Contenu | État |
|-------|---------|------|
| 1 | Fondations : solution, WPF, MVVM, injection, PostgreSQL, EF Core, journalisation | **Terminée** |
| 2 | Authentification et utilisateurs | À venir |
| 3 | Matières, fournisseurs, achats, stock | À venir |
| 4 | Produits, variantes, images, recettes | À venir |
| 5 | Production | À venir |
| 6 | Séchage, fours, cuisson | À venir |
| 7 | Décoration et contrôle qualité | À venir |
| 8 | Clients et commandes personnalisées | À venir |
| 9 | Ventes, paiements, factures | À venir |
| 10 | Dépenses et rapports | À venir |
| 11 | Codes QR, codes-barres, impression | À venir |
| 12 | Sauvegarde, sécurité, optimisation | À venir |
| 13 | Tests, installateur, déploiement | À venir |

---

## Documentation

| Document | Contenu |
|----------|---------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Couches, dépendances, règles |
| [docs/BASE-DE-DONNEES.md](docs/BASE-DE-DONNEES.md) | Arbre des 49 tables et relations |
| [docs/NAVIGATION.md](docs/NAVIGATION.md) | Menu et fonctionnement des écrans |
| [docs/PLAN-DE-DEVELOPPEMENT.md](docs/PLAN-DE-DEVELOPPEMENT.md) | Les 13 étapes |

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
copy src\CeramiPro.App\appsettings.Development.exemple.json src\CeramiPro.App\appsettings.Development.json
```

Renseignez le mot de passe PostgreSQL dans ce fichier, puis :

```bat
dotnet run --project src\CeramiPro.App
```

La base `CeramiProDB` et ses tables sont créées au premier démarrage.

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
```

Les tests qui ont besoin de PostgreSQL sont ignorés si aucun serveur ne
répond, plutôt que déclarés en échec. Pour viser un autre serveur, définissez
la variable `CERAMIPRO_TEST_DB`.

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
