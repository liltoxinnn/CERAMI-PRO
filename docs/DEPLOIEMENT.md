# Déploiement de CERAMIPRO

Ce document décrit l'installation sur l'ordinateur de l'atelier, la sauvegarde
et la restauration des données.

---

## 1. Ce qu'il faut sur la machine

| Élément | Version | Rôle |
|---------|---------|------|
| .NET 8 (ASP.NET Core Runtime) | 8.0 ou supérieur | Exécution du logiciel |
| PostgreSQL | 14 ou supérieur | Stockage des données |

Un ordinateur de bureau ordinaire suffit. Les postes et téléphones du personnel
n'ont besoin que d'un navigateur récent.

---

## 2. Préparer la base de données

```bash
sudo -u postgres createuser --pwprompt ceramipro
sudo -u postgres createdb --owner=ceramipro CeramicWorkshopDB
```

Le logiciel crée lui-même les tables au premier démarrage.

---

## 3. Publier le logiciel

Le logiciel se compose de deux programmes : le serveur applicatif (API) et
l'interface web.

```bash
dotnet publish src/CeramicWorkshop.API -c Release -o /opt/ceramipro/api
dotnet publish src/CeramicWorkshop.Web -c Release -o /opt/ceramipro/web
```

---

## 4. Renseigner les paramètres sensibles

Ces valeurs ne doivent jamais être écrites dans un fichier versionné. Utilisez
des variables d'environnement :

```bash
ConnectionStrings__CeramicWorkshopDB="Host=localhost;Port=5432;Database=CeramicWorkshopDB;Username=ceramipro;Password=MOT_DE_PASSE"
Jwt__Cle="une-cle-secrete-d-au-moins-32-caracteres"
Administrateur__MotDePasseInitial="MotDePasseInitial@2026"
Sauvegarde__Dossier="/var/ceramipro/sauvegardes"
```

Pour l'interface, une seule variable est nécessaire — l'adresse du serveur
applicatif :

```bash
Api__AdresseDeBase="https://ceramipro.local:7035"
```

L'interface ne connaît jamais les identifiants PostgreSQL : elle ne parle
qu'au serveur applicatif.

---

## 5. Lancer les deux services

Exemple de service systemd pour le serveur applicatif :

```ini
[Unit]
Description=CERAMIPRO - serveur applicatif
After=network.target postgresql.service

[Service]
WorkingDirectory=/opt/ceramipro/api
ExecStart=/usr/bin/dotnet /opt/ceramipro/api/CeramicWorkshop.API.dll
Restart=always
RestartSec=10
User=ceramipro
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://0.0.0.0:5130
EnvironmentFile=/etc/ceramipro/api.env

[Install]
WantedBy=multi-user.target
```

Dupliquez ce fichier pour l'interface en changeant le dossier, la DLL et le port.

```bash
sudo systemctl enable --now ceramipro-api ceramipro-web
```

---

## 6. Première connexion

| Champ | Valeur |
|-------|--------|
| Nom d'utilisateur | `admin` |
| Mot de passe | celui de `Administrateur__MotDePasseInitial` |

Le logiciel impose de changer ce mot de passe immédiatement. Créez ensuite un
compte nommé pour chaque personne de l'atelier, avec le rôle qui lui convient.

---

## 7. Sauvegarde

### Depuis le logiciel

L'écran « Sauvegardes » crée une archive `.zip` contenant une copie de chaque
table au format CSV, lisible dans un tableur. Les archives sont écrites dans le
dossier indiqué par `Sauvegarde__Dossier`.

La sauvegarde automatique quotidienne s'active dans « Paramètres », rubrique
Sauvegarde : heure de déclenchement et durée de conservation.

**Copiez régulièrement ces archives sur un support extérieur à l'ordinateur de
l'atelier.** Une sauvegarde restée sur le disque qui tombe en panne ne sert à rien.

### Sauvegarde complète de la base

L'archive CSV permet de tout relire et de tout ressaisir, mais la copie
technique complète — la seule qui se restaure d'un seul geste — se fait avec
l'outil fourni par PostgreSQL :

```bash
pg_dump --format=custom --file=/var/ceramipro/sauvegardes/ceramipro-$(date +%F).dump \
  --username=ceramipro CeramicWorkshopDB
```

Automatisez-la avec une tâche `cron` quotidienne :

```cron
0 22 * * * pg_dump --format=custom --username=ceramipro \
  --file=/var/ceramipro/sauvegardes/ceramipro-$(date +\%F).dump CeramicWorkshopDB
```

---

## 8. Restauration

> Une restauration remplace les données existantes. Faites d'abord une copie de
> l'état actuel, puis arrêtez les deux services.

```bash
sudo systemctl stop ceramipro-api ceramipro-web

# Repartir d'une base vide
sudo -u postgres dropdb CeramicWorkshopDB
sudo -u postgres createdb --owner=ceramipro CeramicWorkshopDB

# Restaurer la copie choisie
pg_restore --username=ceramipro --dbname=CeramicWorkshopDB \
  /var/ceramipro/sauvegardes/ceramipro-2026-08-30.dump

sudo systemctl start ceramipro-api ceramipro-web
```

Vérifiez ensuite dans le logiciel : le tableau de bord, le stock d'un produit
connu et la dernière vente enregistrée.

Les archives CSV, elles, servent à consulter ou à réimporter des données ligne
par ligne dans un tableur ; elles ne se restaurent pas automatiquement.

---

## 9. Mise à jour

```bash
sudo systemctl stop ceramipro-api ceramipro-web
# sauvegarde préalable, puis publication de la nouvelle version
sudo systemctl start ceramipro-api ceramipro-web
```

Les migrations de base de données s'appliquent automatiquement au démarrage du
serveur applicatif. Ce comportement se désactive avec
`BaseDeDonnees__MigrerAuDemarrage=false`, les migrations étant alors à lancer à
la main avec `dotnet ef database update`.

---

## 10. Points de vigilance

* **Chiffrer l'accès** — servez les deux programmes en HTTPS ; le mot de passe
  et le jeton de session circulent sur le réseau de l'atelier.
* **Restreindre l'accès réseau** — PostgreSQL n'a aucune raison d'être joignable
  depuis l'extérieur de la machine.
* **Un compte par personne** — le journal d'audit n'a de valeur que si chacun
  travaille sous son propre nom.
* **Vérifier les sauvegardes** — une sauvegarde jamais relue n'est pas une
  sauvegarde. Testez une restauration au moins une fois.
