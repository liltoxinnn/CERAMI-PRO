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
  5. que chaque « {Binding ...Command} » vise une commande de la vue-modèle ;
  6. que chaque « {Binding ... } » d'un écran vise une propriété qui existe.
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


# ------------------------------------ 6. Propriétés liées d'un écran

def membres_du_type(nom_type, index, vus=None):
    """Propriétés et commandes d'une vue-modèle, héritage compris.

    Les fichiers sont analysés comme du texte : la couche présentation se
    compile, mais l'assemblage n'expose pas les membres engendrés par
    CommunityToolkit sous une forme lisible depuis ce script."""
    vus = vus or set()

    if nom_type in vus or nom_type not in index:
        return set()

    vus.add(nom_type)
    code, base = index[nom_type]
    membres = set()

    # Propriétés écrites à la main.
    for nom in re.findall(
            r'public\s+(?:override\s+|virtual\s+|abstract\s+|static\s+|new\s+)*'
            r'[\w<>?\[\]\(\),\s\.]+?\s(\w+)\s*(?:=>|\{)', code):
        membres.add(nom)

    # Propriétés engendrées : « private bool _obligatoire; » donne « Obligatoire ».
    for nom in re.findall(r'\[ObservableProperty\][^;]*?_(\w+)\s*[;=]', code, re.S):
        membres.add(nom[0].upper() + nom[1:])

    # Commandes engendrées.
    for methode in re.findall(
            r'\[RelayCommand[^\]]*\]\s*(?:private|public|protected|internal)?'
            r'[\w\s<>?]*?\s(\w+)\s*\(', code):
        nom = methode[:-5] if methode.endswith('Async') else methode
        membres.add(nom + 'Command')

    if base:
        membres |= membres_du_type(base, index, vus)

    return membres


def index_des_vues_modeles():
    """Associe chaque vue-modèle à son code et au nom de sa classe de base."""
    index = {}

    for chemin in glob.glob(os.path.join(PRESENTATION, '**', '*.cs'), recursive=True):
        if os.sep + 'obj' + os.sep in chemin:
            continue

        code = lire(chemin)

        for trouve in re.finditer(
                r'class\s+(\w+)\s*(?:<[^>]+>)?\s*(?::\s*([\w<>\.]+))?', code):
            nom, base = trouve.group(1), trouve.group(2)

            if base:
                base = base.split('<')[0].split('.')[-1]

            index[nom] = (code, base)

    return index


# Les vues partagées ne figurent pas dans le fichier des gabarits sous une
# forme exploitable — elles servent plusieurs écrans — mais leurs liaisons
# portent toutes sur la classe de base commune, qu'il faut vérifier : ce sont
# les vues employées par la quasi-totalité des écrans.
VUES_PARTAGEES = {
    'VueListe': 'ListeVueModele',
    'VueDocument': 'DocumentLignesVueModele',
    'FenetreFormulaire': 'FormulaireVueModele',
    'FenetreConnexion': 'ConnexionVueModele',
    'FenetrePrincipale': 'FenetrePrincipaleVueModele',
    'FenetreMotDePasse': 'ChangementMotDePasseVueModele',
    'TableauDeBordVue': 'TableauDeBordVueModele',
}


def vues_modeles_des_ecrans():
    """Associe chaque écran XAML à la vue-modèle que le fichier des vues lui attribue."""
    gabarits = os.path.join(APP, 'Ressources', 'Vues.xaml')

    if not os.path.exists(gabarits):
        return {}

    contenu = lire(gabarits)
    association = {}

    for vueModele, vue in re.findall(
            r'\{x:Type (?:ecrans|vm):(\w+)\}"\s*>\s*<vues:(\w+)\s*/>', contenu):
        # Les vues partagées servent plusieurs écrans : leurs liaisons ne
        # peuvent pas être rattachées à une vue-modèle unique.
        association.setdefault(vue, []).append(vueModele)

    liees = {vue: modeles[0] for vue, modeles in association.items() if len(modeles) == 1}
    liees.update(VUES_PARTAGEES)

    return liees


def liaisons_hors_gabarit(chemin):
    """Chemins liés au contexte de l'écran, hors modèles de données.

    Une liaison placée dans un « DataTemplate » ou dans une colonne de
    tableau porte sur l'élément de la liste, pas sur l'écran : les inclure
    produirait de fausses alertes."""
    document = xml.dom.minidom.parse(chemin)
    liaisons = set()

    def parcourir(noeud):
        if noeud.nodeType != noeud.ELEMENT_NODE:
            return

        nom = noeud.tagName

        if (nom.endswith('DataTemplate')
                or nom.endswith('ItemsPanelTemplate')
                or nom.endswith('ControlTemplate')
                or 'Column' in nom):
            return

        if noeud.hasAttributes():
            for rang in range(noeud.attributes.length):
                valeur = noeud.attributes.item(rang).value

                for chemin_lie in re.findall(r'\{Binding\s+([^},\s]+)', valeur):
                    liaisons.add(chemin_lie)

        for enfant in noeud.childNodes:
            parcourir(enfant)

    parcourir(document.documentElement)
    return liaisons


def verifier_liaisons(chemin, ecrans, index):
    nom_vue = os.path.splitext(os.path.basename(chemin))[0]

    if nom_vue not in ecrans:
        return

    vue_modele = ecrans[nom_vue]
    membres = membres_du_type(vue_modele, index)

    if not membres:
        return

    for lie in liaisons_hors_gabarit(chemin):
        # Une liaison composée vise une propriété d'une propriété : seule la
        # première est vérifiable ici.
        premier = lie.split('.')[0].split('[')[0]

        if not premier or not premier[0].isupper():
            continue

        if premier not in membres:
            signaler(chemin, 'liaison « %s » absente de %s' % (premier, vue_modele))


def main():
    connues = ressources_declarees()
    commandes = commandes_declarees()
    index = index_des_vues_modeles()
    ecrans = vues_modeles_des_ecrans()

    fichiers = fichiers_xaml()

    for chemin in fichiers:
        if not verifier_bien_forme(chemin):
            continue

        contenu = lire(chemin)

        verifier_classe(chemin, contenu)
        verifier_ressources(chemin, contenu, connues)
        verifier_evenements(chemin, contenu)
        verifier_commandes(chemin, contenu, commandes)
        verifier_liaisons(chemin, ecrans, index)

    print('%d fichiers XAML, %d ressources, %d commandes, %d écrans liés'
          % (len(fichiers), len(connues), len(commandes), len(ecrans)))

    if erreurs:
        print('\n%d problème(s) :' % len(erreurs))
        for erreur in erreurs:
            print('  - ' + erreur)
        return 1

    print('Aucun problème détecté.')
    return 0


if __name__ == '__main__':
    sys.exit(main())
