# Architecture de CeramiPro

## Nature du logiciel

Application **Windows native** en WPF. Elle s'installe, se lance depuis une
icône du bureau et fonctionne sans navigateur, sans serveur web et sans
connexion Internet. Les données vivent dans PostgreSQL, installé sur le même
poste ou sur un ordinateur de l'atelier.

```
Bureau  →  CeramiPro.exe  →  Fenêtre Windows  →  PostgreSQL (local)
```

---

## Les couches

```
┌─────────────────────────────────────────────────────────┐
│  CeramiPro.App           Fenêtres et écrans (XAML)      │  Windows uniquement
│                          Aucune logique métier          │
├─────────────────────────────────────────────────────────┤
│  CeramiPro.Presentation  Vues-modèles (MVVM)            │  Testable
│                          Navigation, commandes, état    │
├─────────────────────────────────────────────────────────┤
│  CeramiPro.Application   Règles de gestion, services    │  Testable
│                          Contrats (interfaces), DTO     │
├─────────────────────────────────────────────────────────┤
│  CeramiPro.Infrastructure  PostgreSQL, EF Core,         │  Testable
│                            fichiers, impression         │
├─────────────────────────────────────────────────────────┤
│  CeramiPro.Domain        Entités et énumérations        │  Testable
│                          Aucune dépendance              │
└─────────────────────────────────────────────────────────┘
```

Les dépendances ne vont que vers le bas. `Domain` ne connaît personne ;
`Application` définit des interfaces que `Infrastructure` implémente. C'est ce
qui permet de tester les règles métier sans base de données.

### Pourquoi un projet « Presentation » séparé

Le cahier des charges prévoyait quatre projets, les vues-modèles étant dans
`CeramiPro.App`. Elles ont été sorties dans un cinquième projet pour une raison
concrète : **un projet WPF ne se compile que sous Windows**. En isolant les
vues-modèles, la logique d'interface — navigation, droits affichés, validation
des saisies, enchaînement des écrans — devient vérifiable par des tests
automatiques sur n'importe quelle machine.

`CeramiPro.App` ne contient donc que du XAML et le peu de code qui l'accompagne.

---

## Règles d'architecture

1. **Aucune logique métier dans les vues.** Un fichier `.xaml.cs` ne contient
   que `InitializeComponent()`.
2. **Aucun accès direct à la base depuis une vue-modèle.** Elle appelle un
   service de la couche Application.
3. **Toute modification de stock passe par le service de stock.** Aucune
   vue-modèle ne touche une quantité.
4. **Les droits sont vérifiés dans les services**, pas seulement en masquant
   des boutons.
5. **Les opérations qui touchent plusieurs tables sont transactionnelles.**
   Une vente crée la vente, ses lignes, le paiement, les mouvements de stock et
   la facture : tout réussit, ou rien.
6. **Les entités sont nommées en anglais, l'interface est en français.** Les
   libellés affichés viennent d'attributs `[Libelle("…")]`, jamais du nom
   technique.

---

## Journalisation et erreurs

Serilog écrit un fichier par jour dans :

```
%LOCALAPPDATA%\CeramiPro\journaux\ceramipro-AAAAMMJJ.log
```

L'utilisateur ne voit jamais une exception technique : il reçoit un message en
français expliquant ce qui n'a pas fonctionné. Le détail part dans le journal.

---

## Dossier de travail

```
%LOCALAPPDATA%\CeramiPro\
├── journaux\        Fichiers de journalisation
├── images\          Photos produits, commandes, production, qualité
│   ├── produits\
│   ├── commandes\
│   ├── production\
│   └── qualite\
├── documents\       Factures et reçus générés
└── sauvegardes\     Archives de la base
```

Les fichiers sont sur le disque, leurs références en base : une base de
données n'est pas faite pour stocker des photos.
