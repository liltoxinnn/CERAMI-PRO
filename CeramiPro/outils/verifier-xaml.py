#!/usr/bin/env python3
# coding: utf-8
"""
Vérification statique des fichiers XAML de CeramiPro.

WPF ne se compile pas sous Linux : ce script rattrape ce que le compilateur
XAML signalerait, afin qu'aucune erreur ne soit découverte au clic, sur la
machine de l'atelier.

Il vérifie :
  1. que chaque fichier est un XML bien formé ;
  2. que chaque « x:Class » correspond à une classe partielle existante ;
  3. que chaque « {StaticResource ...} » désigne une ressource déclarée ;
  4. que chaque gestionnaire d'événement nommé existe dans le code compagnon ;
  5. que chaque « {Binding ...Command} » vise une commande de la vue-modèle.
"""

import glob
import os
import re
import sys
import xml.dom.minidom

RACINE = os.path.join(os.path.dirname(os.path.abspath(__file__)), '..')
APP = os.path.join(RACINE, 'src', 'CeramiPro.App')
PRESENTATION = os.path.join(RACINE, 'src', 'CeramiPro.Presentation')

erreurs = []


def signaler(fichier, message):
    erreurs.append('%s : %s' % (os.path.relpath(fichier, RACINE), message))


def fichiers_xaml():
    return sorted(glob.glob(os.path.join(APP, '**', '*.xaml'), recursive=True))


def lire(chemin):
    with open(chemin, encoding='utf-8') as f:
        return f.read()


# ------------------------------------------------- 1. XML bien formé

def verifier_bien_forme(chemin):
    try:
        xml.dom.minidom.parse(chemin)
        return True
    except Exception as erreur:
        signaler(chemin, 'XML mal formé — %s' % erreur)
        return False


# ------------------------------------- 2. Classes partielles compagnon

def verifier_classe(chemin, contenu):
    trouve = re.search(r'x:Class="([\w.]+)"', contenu)

    if not trouve:
        return

    nom_complet = trouve.group(1)
    espace, _, classe = nom_complet.rpartition('.')
    compagnon = chemin + '.cs'

    if not os.path.exists(compagnon):
        signaler(chemin, 'aucun fichier compagnon « %s »' % os.path.basename(compagnon))
        return

    code = lire(compagnon)

    if ('namespace %s;' % espace) not in code and ('namespace %s' % espace) not in code:
        signaler(compagnon, 'espace de noms attendu « %s »' % espace)

    if not re.search(r'partial class %s\b' % re.escape(classe), code):
        signaler(compagnon, 'classe partielle « %s » introuvable' % classe)


# --------------------------------------------- 3. Ressources statiques

def ressources_declarees():
    cles = set()

    for chemin in fichiers_xaml():
        contenu = lire(chemin)

        for cle in re.findall(r'x:Key="([^"]+)"', contenu):
            cles.add(cle)

    return cles


def verifier_ressources(chemin, contenu, connues):
    # Les ressources déclarées dans le fichier lui-même comptent aussi.
    locales = set(re.findall(r'x:Key="([^"]+)"', contenu))

    for cle in set(re.findall(r'\{StaticResource\s+([^}\s,]+)', contenu)):
        if cle not in connues and cle not in locales:
            signaler(chemin, 'ressource « %s » introuvable' % cle)


# ----------------------------------------- 4. Gestionnaires d'événements

EVENEMENTS = ('Click', 'MouseDoubleClick', 'SelectionChanged', 'KeyDown',
              'TextChanged', 'Loaded', 'Checked', 'Unchecked')


def verifier_evenements(chemin, contenu):
    compagnon = chemin + '.cs'

    if not os.path.exists(compagnon):
        return

    code = lire(compagnon)

    for evenement in EVENEMENTS:
        for methode in re.findall(r'\b%s="(\w+)"' % evenement, contenu):
            if not re.search(r'\b%s\s*\(' % re.escape(methode), code):
                signaler(chemin, 'gestionnaire « %s » absent du code compagnon' % methode)


# ----------------------------------------------- 5. Commandes liées

def commandes_declarees():
    """Toutes les commandes exposées par les vues-modèles.

    CommunityToolkit engendre « FaireCommand » à partir de « FaireAsync » ou
    de « Faire » : la génération est reproduite ici, faute de pouvoir lire
    l'assemblage engendré."""
    commandes = set()

    for chemin in glob.glob(os.path.join(PRESENTATION, '**', '*.cs'), recursive=True):
        if os.sep + 'obj' + os.sep in chemin:
            continue

        code = lire(chemin)

        for methode in re.findall(
                r'\[RelayCommand[^\]]*\]\s*(?:private|public|protected|internal)?'
                r'[\w\s<>?]*?\s(\w+)\s*\(', code):
            nom = methode[:-5] if methode.endswith('Async') else methode
            commandes.add(nom + 'Command')

    return commandes


def verifier_commandes(chemin, contenu, connues):
    for liaison in set(re.findall(r'Command="\{Binding\s+([^}\s,]+)', contenu)):
        nom = liaison.split('.')[-1]

        if not nom.endswith('Command'):
            continue

        if nom not in connues:
            signaler(chemin, 'commande « %s » introuvable dans les vues-modèles' % nom)


def main():
    connues = ressources_declarees()
    commandes = commandes_declarees()

    fichiers = fichiers_xaml()

    for chemin in fichiers:
        if not verifier_bien_forme(chemin):
            continue

        contenu = lire(chemin)

        verifier_classe(chemin, contenu)
        verifier_ressources(chemin, contenu, connues)
        verifier_evenements(chemin, contenu)
        verifier_commandes(chemin, contenu, commandes)

    print('%d fichiers XAML, %d ressources, %d commandes'
          % (len(fichiers), len(connues), len(commandes)))

    if erreurs:
        print('\n%d problème(s) :' % len(erreurs))
        for erreur in erreurs:
            print('  - ' + erreur)
        return 1

    print('Aucun problème détecté.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
