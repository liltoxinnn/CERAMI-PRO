# CERAMIPRO — Logiciel de gestion d'atelier de céramique

Logiciel professionnel de gestion pour un atelier de céramique et de décoration
artisanale : matières premières, production, séchage, cuisson, décoration,
contrôle qualité, produits finis, commandes clients, paiements et livraison.

**Interface entièrement en français. Configuration par défaut pour l'Algérie**
(dinar algérien, format `45 000,00 DA`, dates `JJ/MM/AAAA`, fuseau `Africa/Algiers`).

---

## État d'avancement

| Étape | Contenu | État |
|-------|---------|------|
| 1 | Architecture, base de données, sécurité, utilisateurs, paramètres | **Terminée** |
| 2 | Stock : matières, unités, fournisseurs, achats, mouvements | À venir |
| 3 | Produits : catalogue, photos, variantes, recettes | À venir |
| 4 | Production : ordres, étapes, consommation des matières | À venir |
| 5 | Cuisson, décoration, contrôle qualité | À venir |
| 6 | Clients et commandes personnalisées | À venir |
| 7 | Ventes, factures, paiements, dettes | À venir |
| 8 | Dépenses, bénéfices, rapports, statistiques | À venir |
| 9 | QR codes, codes-barres, scanner | À venir |
| 10 | Tests finaux, sauvegarde, déploiement | À venir |

Le détail figure dans [docs/PLAN-DE-DEVELOPPEMENT.md](docs/PLAN-DE-DEVELOPPEMENT.md).

---

## Documentation

| Document | Contenu |
|----------|---------|
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | Couches, dépendances, structure des dossiers |
| [docs/BASE-DE-DONNEES.md](docs/BASE-DE-DONNEES.md) | Arbre des tables, relations, diagramme ERD |
| [docs/REGLES-METIER.md](docs/REGLES-METIER.md) | Les 20 règles métier et leur mise en œuvre |
| [docs/PLAN-DE-DEVELOPPEMENT.md](docs/PLAN-DE-DEVELOPPEMENT.md) | Les 10 étapes de développement |

---

## Prérequis

* [.NET SDK 8.0](https://dotnet.microsoft.com/download) ou supérieur
* [PostgreSQL 14](https://www.postgresql.org/download/) ou supérieur

---

## Installation

### 1. Récupérer le projet

```bash
git clone <adresse-du-depot>
cd CeramicWorkshop
```

### 2. Configurer la connexion à PostgreSQL

Les paramètres sensibles ne sont pas versionnés. Copiez les fichiers d'exemple :

```bash
cp src/CeramicWorkshop.API/appsettings.Development.exemple.json \
   src/CeramicWorkshop.API/appsettings.Development.json

cp src/CeramicWorkshop.Web/appsettings.Development.exemple.json \
   src/CeramicWorkshop.Web/appsettings.Development.json
```

puis renseignez le mot de passe PostgreSQL, la clé de signature des jetons
et le mot de passe initial de l'administrateur.

Vous pouvez également utiliser les secrets de développement, qui restent
hors du dossier du projet :

```bash
dotnet user-secrets --project src/CeramicWorkshop.API set \
  "ConnectionStrings:CeramicWorkshopDB" \
  "Host=localhost;Port=5432;Database=CeramicWorkshopDB;Username=postgres;Password=VOTRE_MOT_DE_PASSE"

dotnet user-secrets --project src/CeramicWorkshop.API set \
  "Jwt:Cle" "une-cle-secrete-d-au-moins-32-caracteres"

dotnet user-secrets --project src/CeramicWorkshop.API set \
  "Administrateur:MotDePasseInitial" "VotreMotDePasse@2026"
```

En production, utilisez plutôt des variables d'environnement :
`ConnectionStrings__CeramicWorkshopDB`, `Jwt__Cle`, `Administrateur__MotDePasseInitial`.

### 3. Démarrer le serveur applicatif (API)

```bash
dotnet run --project src/CeramicWorkshop.API
```

Au premier démarrage, le logiciel crée la base `CeramicWorkshopDB`, applique les
migrations et enregistre les données de départ : rôles, droits, unités de mesure,
modes de règlement, catégories et compte administrateur.

La documentation de l'API est disponible sur `/swagger` en développement.

### 4. Démarrer l'interface

Dans un second terminal :

```bash
dotnet run --project src/CeramicWorkshop.Web
```

Ouvrez ensuite l'adresse affichée dans la console (par défaut `https://localhost:7225`).

### 5. Première connexion

| Champ | Valeur |
|-------|--------|
| Nom d'utilisateur | `admin` |
| Mot de passe | celui défini dans `Administrateur:MotDePasseInitial` (à défaut `CeramiPro@2026`) |

Le logiciel demande de changer ce mot de passe dès la première connexion.

---

## Tests

```bash
# Tests unitaires (aucune base de données nécessaire)
dotnet test tests/UnitTests

# Tests d'intégration (nécessitent PostgreSQL)
dotnet test tests/IntegrationTests
```

Les tests d'intégration utilisent la base `CeramicWorkshopDB_Tests`. Pour viser un
autre serveur, définissez la variable `CERAMIPRO_TEST_DB`. Sans serveur joignable,
ces tests sont ignorés au lieu d'échouer.

---

## Migrations de la base de données

```bash
# Ajouter une migration
dotnet ef migrations add NomDeLaMigration \
  --project src/CeramicWorkshop.Infrastructure \
  --startup-project src/CeramicWorkshop.API \
  --output-dir Migrations

# Appliquer les migrations
dotnet ef database update \
  --project src/CeramicWorkshop.Infrastructure \
  --startup-project src/CeramicWorkshop.API
```

Les migrations s'appliquent aussi automatiquement au démarrage de l'API.
Ce comportement se désactive avec `BaseDeDonnees:MigrerAuDemarrage = false`.

---

## Rôles livrés avec le logiciel

| Rôle | Accès |
|------|-------|
| **Administrateur** | Accès complet, y compris comptes, paramètres, journal et sauvegardes |
| **Responsable** | Ventes, production, stock, clients, fournisseurs, rapports |
| **Employé** | Production, étapes de fabrication, cuisson, décoration, qualité |
| **Caissier** | Ventes, factures, paiements, clients |

Les droits de chaque rôle se modifient dans l'écran « Rôles et droits ».
Le rôle « Administrateur » conserve toujours l'intégralité des droits.

---

## Sécurité

* Mots de passe hachés avec PBKDF2 (HMAC-SHA512, 210 000 itérations, sel aléatoire).
* Authentification par jeton JWT signé, renouvellement par jeton dédié.
* Autorisation par droit fonctionnel, vérifiée à chaque appel de l'API.
* Blocage temporaire du compte après cinq échecs de connexion.
* Requêtes paramétrées via Entity Framework Core (aucune concaténation SQL).
* Opérations importantes journalisées dans `AuditLogs`.
* Documents financiers supprimés logiquement : l'historique reste consultable.
* Les identifiants PostgreSQL ne sont jamais exposés à l'interface : celle-ci
  ne communique qu'avec l'API.
