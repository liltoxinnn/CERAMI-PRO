# Architecture de CERAMIPRO

## 1. Principe général

Le logiciel suit une **architecture en couches** (Clean Architecture). Chaque couche
ne connaît que celles situées en dessous d'elle : les règles métier ne dépendent ni
de la base de données, ni de l'interface. Changer PostgreSQL pour un autre moteur,
ou l'interface web pour une application mobile, ne remet pas en cause le cœur du logiciel.

```
┌──────────────────────────────────────────────────────────────┐
│  CeramicWorkshop.Web            Interface (Blazor, français) │
│  Pages, composants, services d'appel, session utilisateur    │
└───────────────────────────┬──────────────────────────────────┘
                            │ HTTPS + jeton JWT
┌───────────────────────────▼──────────────────────────────────┐
│  CeramicWorkshop.API            Points d'entrée HTTP         │
│  Contrôleurs, filtres, middleware, autorisation par droit    │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  CeramicWorkshop.Application    Règles métier                │
│  Services, DTOs, validateurs, interfaces                     │
└───────────────────────────┬──────────────────────────────────┘
                            │
┌───────────────────────────▼──────────────────────────────────┐
│  CeramicWorkshop.Domain         Cœur du métier               │
│  Entités, énumérations, catalogue des droits                 │
└──────────────────────────────────────────────────────────────┘
                            ▲
┌───────────────────────────┴──────────────────────────────────┐
│  CeramicWorkshop.Infrastructure Détails techniques           │
│  Entity Framework Core, PostgreSQL, sécurité, horloge, audit │
└──────────────────────────────────────────────────────────────┘
```

`Infrastructure` implémente les interfaces déclarées par `Application` : c'est
l'inversion de dépendance. La couche métier ignore totalement Entity Framework.

## 2. Rôle de chaque couche

### CeramicWorkshop.Domain

Le vocabulaire de l'atelier, sans aucune dépendance technique.

* `Entities/` — 45 entités réparties par module (Identity, Materials, Production, Firing…).
* `Enums/` — 22 énumérations métier, chacune portant son libellé français
  (`[Libelle("Première cuisson")]`) affiché tel quel dans l'interface.
* `Common/` — classes de base (`BaseEntity`, `AuditableEntity`, `ISoftDeletable`)
  et catalogue des droits (`PermissionCodes`, `RoleCodes`).

### CeramicWorkshop.Application

Les règles métier et les contrats.

* `Interfaces/` — `IApplicationDbContext`, `ICurrentUserService`, `IDateTimeService`,
  `IPasswordHasherService`, `ITokenService`, `IAuditService`, plus les interfaces de service.
* `Services/` — la logique métier : `AuthService`, `UtilisateurService`, `RoleService`,
  `ParametresService`.
* `DTOs/` — objets échangés avec l'interface, nommés en français.
* `Validators/` — validation FluentValidation, messages rédigés en français.
* `Common/` — `Result`, `PagedResult`, exceptions métier, `MontantFormatter`.

### CeramicWorkshop.Infrastructure

Les détails techniques, interchangeables.

* `Data/` — `ApplicationDbContext`, conversion systématique des dates en UTC,
  remplissage automatique de la traçabilité, suppression logique.
* `Configurations/` — configuration Entity Framework de chaque table
  (longueurs, précisions, index uniques, comportements de suppression).
* `Data/Seed/` — `DatabaseSeeder` : rôles, droits, administrateur, unités,
  modes de règlement, catégories. Réexécutable sans effet de bord.
* `Authentication/` — hachage PBKDF2, génération des jetons JWT, utilisateur courant.
* `Services/` — horloge de l'atelier (fuseau `Africa/Algiers`), journal d'audit.
* `Migrations/` — migrations PostgreSQL générées par Entity Framework Core.

### CeramicWorkshop.API

La porte d'entrée du logiciel.

* `Controllers/` — contrôleurs minces : ils appellent un service et renvoient le résultat.
* `Authorization/` — une règle d'accès par droit du catalogue,
  utilisée via `[DroitRequis(PermissionCodes.VentesCreer)]`.
* `Filters/` — validation automatique des formulaires reçus.
* `Middleware/` — transformation de toute exception en message français,
  sans jamais exposer de détail technique au navigateur.

### CeramicWorkshop.Web

L'interface utilisée quotidiennement par l'atelier.

* `Components/Pages/` — pages : connexion, tableau de bord, utilisateurs,
  rôles et droits, paramètres, mon compte.
* `Components/Layout/` — mise en page, menu principal, en-tête.
* `Components/Shared/` — composants réutilisables : chargement, messages,
  résumé d'erreurs, fenêtre de confirmation.
* `Services/` — `ClientApi` (appels HTTP avec jeton et renouvellement automatique),
  `ServiceAuthentification`, `FournisseurEtatAuthentification`, `ServiceMessages`.
* `Models/` — session utilisateur, entrées de menu, résultat d'appel.
* `wwwroot/app.css` — thème complet, responsive, sans dépendance externe.

## 3. Structure des dossiers

```
CeramicWorkshop
│
├── src
│   ├── CeramicWorkshop.Domain
│   │   ├── Common            Classes de base et catalogue des droits
│   │   ├── Entities          45 entités réparties en 20 modules
│   │   └── Enums             22 énumérations avec libellés français
│   │
│   ├── CeramicWorkshop.Application
│   │   ├── Common            Result, pagination, exceptions, formatage
│   │   ├── DTOs              Auth, Identity, Settings
│   │   ├── Interfaces        Contrats implémentés par Infrastructure
│   │   ├── Services          Règles métier
│   │   └── Validators        Validation des formulaires
│   │
│   ├── CeramicWorkshop.Infrastructure
│   │   ├── Authentication    Hachage, jetons, utilisateur courant
│   │   ├── Configurations    Configuration EF Core de chaque table
│   │   ├── Data              Contexte, conversion UTC, semis initial
│   │   ├── Migrations        Migrations PostgreSQL
│   │   └── Services          Horloge de l'atelier, journal d'audit
│   │
│   ├── CeramicWorkshop.API
│   │   ├── Authorization     Règles d'accès par droit
│   │   ├── Controllers       Points d'entrée HTTP
│   │   ├── Filters           Validation automatique
│   │   ├── Middleware        Gestion des erreurs en français
│   │   └── Program.cs        Démarrage, sécurité, migrations
│   │
│   └── CeramicWorkshop.Web
│       ├── Components        Pages, mise en page, composants partagés
│       ├── Models            Session, menu, résultats
│       ├── Services          Appels API, authentification, messages
│       └── wwwroot           Feuille de style et ressources
│
├── tests
│   ├── UnitTests             Règles métier, sans base de données
│   └── IntegrationTests      API complète sur PostgreSQL
│
├── docs                      Documentation du projet
└── README.md
```

## 4. Choix techniques et raisons

| Choix | Raison |
|-------|--------|
| **Blazor Server** | Une seule technologie (C#) du serveur à l'écran ; installation simple pour un atelier. |
| **API séparée** | Prépare l'application mobile, la boutique en ligne et le catalogue public sans réécriture. |
| **PBKDF2 (HMAC-SHA512)** | Fourni par la plateforme, sans bibliothèque tierce, conforme aux recommandations actuelles. |
| **Jeton JWT** | Authentification sans état : plusieurs postes ou ateliers peuvent partager le même serveur. |
| **Droits fins plutôt que rôles figés** | Le propriétaire ajuste ce que voit chaque employé sans intervention informatique. |
| **Dates stockées en UTC** | Évite tout décalage lors d'un changement d'heure ou d'un serveur hébergé ailleurs. |
| **Suppression logique des documents financiers** | Une facture ou un paiement reste consultable même après annulation. |
| **Devise et langue en paramètre** | La traduction arabe et une autre devise ne demanderont aucune modification du code. |

## 5. Traitement des langues et des formats

* Toute l'interface, les messages d'erreur et les libellés d'état sont en français.
* Les libellés des états métier sont portés par le domaine
  (`ProductionStatus.Faconnage` → « Façonnage »), donc partagés par l'interface,
  les rapports et les futurs documents PDF.
* Les montants sont formatés par `MontantFormatter` : `45 000,00 DA`,
  avec un espace insécable comme séparateur de milliers.
* Le symbole de la devise, le nombre de décimales, le format de date et le fuseau
  horaire proviennent des paramètres de l'atelier, modifiables dans l'interface.

## 6. Sécurité, de bout en bout

1. L'utilisateur saisit ses identifiants dans l'interface.
2. L'API vérifie le mot de passe (PBKDF2), bloque le compte après cinq échecs,
   puis délivre un jeton d'accès contenant le rôle et la liste des droits.
3. L'interface conserve ce jeton dans le stockage chiffré du navigateur,
   jamais dans le code HTML de la page.
4. Chaque appel à l'API est vérifié : jeton valide **et** droit correspondant.
5. L'interface masque en plus les écrans et les boutons non autorisés, par confort.
   La sécurité réelle reste celle de l'API.
6. Les opérations importantes sont inscrites dans `AuditLogs` avec l'auteur,
   la date et l'adresse réseau.
