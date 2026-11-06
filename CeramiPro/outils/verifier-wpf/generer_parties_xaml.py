"""Génère, pour chaque XAML à x:Class, la partie que le compilateur XAML
produirait : InitializeComponent() et un champ par élément nommé.

Sert à compiler le C# de l'application Windows là où le kit WindowsDesktop
n'existe pas. Les éléments nommés à l'intérieur d'un modèle (ControlTemplate,
DataTemplate, Style) n'en font pas partie : ils ne deviennent pas des champs.

Usage : generer_parties_xaml.py <dossier de CeramiPro.App> <dossier de sortie>
"""
import os, re, sys, xml.dom.minidom

app = sys.argv[1]
sortie = sys.argv[2]
os.makedirs(sortie, exist_ok=True)

CONTROLES = 'System.Windows.Controls.'
AUTRES = {'Run': 'System.Windows.Documents.Run', 'Window': 'System.Windows.Window',
          'Ellipse': 'System.Windows.Shapes.Ellipse', 'Rectangle': 'System.Windows.Shapes.Rectangle'}

for racine, _, fichiers in os.walk(app):
    if '/obj' in racine or '/bin' in racine:
        continue
    for nom in fichiers:
        if not nom.endswith('.xaml'):
            continue
        chemin = os.path.join(racine, nom)
        doc = xml.dom.minidom.parse(chemin)
        racine_xml = doc.documentElement
        classe = racine_xml.getAttribute('x:Class')
        if not classe:
            continue
        espace, _, court = classe.rpartition('.')
        champs = []
        for noeud in doc.getElementsByTagName('*'):
            nom_x = noeud.getAttribute('x:Name')
            if not nom_x or noeud is racine_xml:
                continue
            parent, dans_gabarit = noeud.parentNode, False
            while parent is not None and parent.nodeType == parent.ELEMENT_NODE:
                if parent.tagName.endswith('Template') or parent.tagName == 'Style':
                    dans_gabarit = True
                    break
                parent = parent.parentNode
            if dans_gabarit:
                continue
            tag = noeud.tagName
            if ':' in tag:
                prefixe, local = tag.split(':', 1)
                xmlns = racine_xml.getAttribute('xmlns:' + prefixe)
                ns = re.search(r'clr-namespace:([\w.]+)', xmlns).group(1)
                type_ = ns + '.' + local
            else:
                type_ = AUTRES.get(tag, CONTROLES + tag)
            champs.append(f'    internal {type_} {nom_x} = null!;')
        principal = ''
        if racine_xml.tagName == 'Application':
            principal = '    public static void Main() { }\n'
        code = f'''namespace {espace};

public partial class {court}
{{
    public void InitializeComponent() {{ }}
{principal}{chr(10).join(champs)}
}}
'''
        with open(os.path.join(sortie, court + '.g.cs'), 'w', encoding='utf-8') as f:
            f.write(code)
        print(classe, len(champs))
