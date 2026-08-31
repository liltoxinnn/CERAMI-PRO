namespace CeramiPro.Application.Localisation;

/// <summary>
/// Textes de l'interface, dans les langues proposées.
///
/// Le français fait référence : c'est la langue de travail de l'atelier. Une
/// clé sans traduction arabe retombe sur le français, ce qui est préférable à
/// un écran vide ou à un nom technique affiché tel quel.
///
/// Les clés suivent la forme « rubrique.element » pour rester classables.
/// </summary>
public static class Traductions
{
    public static IReadOnlyDictionary<string, string> Francais { get; } = new Dictionary<string, string>
    {
        // ----------------------------------------------------------- Menu
        ["menu.tableauDeBord"] = "Tableau de bord",
        ["menu.stock"] = "Stock",
        ["menu.stock.vueGenerale"] = "Vue générale",
        ["menu.stock.matieres"] = "Matières premières",
        ["menu.stock.produitsFinis"] = "Produits finis",
        ["menu.stock.mouvements"] = "Mouvements",
        ["menu.stock.alertes"] = "Alertes",
        ["menu.produits"] = "Produits",
        ["menu.produits.catalogue"] = "Catalogue",
        ["menu.produits.categories"] = "Catégories",
        ["menu.produits.variantes"] = "Variantes",
        ["menu.produits.recettes"] = "Recettes",
        ["menu.production"] = "Production",
        ["menu.production.ordres"] = "Ordres de production",
        ["menu.production.planning"] = "Planning",
        ["menu.production.enCours"] = "Production en cours",
        ["menu.production.historique"] = "Historique",
        ["menu.cuisson"] = "Cuisson",
        ["menu.cuisson.fours"] = "Fours",
        ["menu.cuisson.lots"] = "Lots de cuisson",
        ["menu.decoration"] = "Décoration",
        ["menu.decoration.travaux"] = "Travaux de décoration",
        ["menu.decoration.types"] = "Types de décoration",
        ["menu.qualite"] = "Contrôle qualité",
        ["menu.clients"] = "Clients",
        ["menu.commandes"] = "Commandes personnalisées",
        ["menu.fournisseurs"] = "Fournisseurs",
        ["menu.ventes"] = "Ventes",
        ["menu.paiements"] = "Paiements",
        ["menu.factures"] = "Factures",
        ["menu.depenses"] = "Dépenses",
        ["menu.rapports"] = "Rapports",
        ["menu.parametres"] = "Paramètres",

        // -------------------------------------------------------- Actions
        ["action.ajouter"] = "Ajouter",
        ["action.modifier"] = "Modifier",
        ["action.supprimer"] = "Supprimer",
        ["action.enregistrer"] = "Enregistrer",
        ["action.annuler"] = "Annuler",
        ["action.rechercher"] = "Rechercher",
        ["action.filtrer"] = "Filtrer",
        ["action.exporter"] = "Exporter",
        ["action.imprimer"] = "Imprimer",
        ["action.valider"] = "Valider",
        ["action.fermer"] = "Fermer",
        ["action.actualiser"] = "Actualiser",
        ["action.retour"] = "Retour",
        ["action.connexion"] = "Se connecter",
        ["action.deconnexion"] = "Se déconnecter",

        // --------------------------------------------------------- États
        ["etat.chargement"] = "Chargement…",
        ["etat.aucunResultat"] = "Aucun résultat",
        ["etat.aucuneDonnee"] = "Aucune donnée pour l'instant",
        ["etat.obligatoire"] = "Champ obligatoire",

        // ------------------------------------------------------ Messages
        ["message.succes"] = "Opération réussie",
        ["message.erreur"] = "Erreur",
        ["message.attention"] = "Attention",
        ["message.confirmation"] = "Confirmation",
        ["message.confirmerSuppression"] = "Voulez-vous vraiment supprimer cet élément ?",
        ["message.stockInsuffisant"] = "Stock insuffisant",
        ["message.enregistre"] = "Enregistré",
        ["message.erreurEnregistrement"] = "Erreur lors de l'enregistrement",
        ["message.sessionAbsente"] = "Aucune session",

        // ------------------------------------------------------- Communs
        ["commun.nom"] = "Nom",
        ["commun.reference"] = "Référence",
        ["commun.categorie"] = "Catégorie",
        ["commun.quantite"] = "Quantité",
        ["commun.prix"] = "Prix",
        ["commun.total"] = "Total",
        ["commun.date"] = "Date",
        ["commun.statut"] = "Statut",
        ["commun.client"] = "Client",
        ["commun.fournisseur"] = "Fournisseur",
        ["commun.produit"] = "Produit",
        ["commun.matiere"] = "Matière",
        ["commun.stock"] = "Stock",
        ["commun.description"] = "Description",
        ["commun.notes"] = "Notes",
        ["commun.utilisateur"] = "Utilisateur",
        ["commun.role"] = "Rôle",
        ["commun.langue"] = "Langue",

        // ------------------------------------------------------ Connexion
        ["connexion.titre"] = "Connexion",
        ["connexion.nomUtilisateur"] = "Nom d'utilisateur",
        ["connexion.motDePasse"] = "Mot de passe",
        ["connexion.echec"] = "Nom d'utilisateur ou mot de passe incorrect.",
        ["connexion.compteBloque"] = "Compte bloqué après plusieurs tentatives. Réessayez plus tard.",
        ["connexion.compteDesactive"] = "Ce compte est désactivé.",

        // -------------------------------------------------- Application
        ["app.nom"] = "CeramiPro",
        ["app.sousTitre"] = "Gestion d'atelier"
    };

    public static IReadOnlyDictionary<string, string> Arabe { get; } = new Dictionary<string, string>
    {
        // ----------------------------------------------------------- Menu
        ["menu.tableauDeBord"] = "لوحة القيادة",
        ["menu.stock"] = "المخزون",
        ["menu.stock.vueGenerale"] = "نظرة عامة",
        ["menu.stock.matieres"] = "المواد الأولية",
        ["menu.stock.produitsFinis"] = "المنتجات الجاهزة",
        ["menu.stock.mouvements"] = "الحركات",
        ["menu.stock.alertes"] = "التنبيهات",
        ["menu.produits"] = "المنتجات",
        ["menu.produits.catalogue"] = "الكتالوج",
        ["menu.produits.categories"] = "الفئات",
        ["menu.produits.variantes"] = "الأنواع",
        ["menu.produits.recettes"] = "الوصفات",
        ["menu.production"] = "الإنتاج",
        ["menu.production.ordres"] = "أوامر الإنتاج",
        ["menu.production.planning"] = "التخطيط",
        ["menu.production.enCours"] = "الإنتاج الجاري",
        ["menu.production.historique"] = "السجل",
        ["menu.cuisson"] = "الحرق",
        ["menu.cuisson.fours"] = "الأفران",
        ["menu.cuisson.lots"] = "دفعات الحرق",
        ["menu.decoration"] = "الزخرفة",
        ["menu.decoration.travaux"] = "أعمال الزخرفة",
        ["menu.decoration.types"] = "أنواع الزخرفة",
        ["menu.qualite"] = "مراقبة الجودة",
        ["menu.clients"] = "الزبائن",
        ["menu.commandes"] = "الطلبات الخاصة",
        ["menu.fournisseurs"] = "الموردون",
        ["menu.ventes"] = "المبيعات",
        ["menu.paiements"] = "المدفوعات",
        ["menu.factures"] = "الفواتير",
        ["menu.depenses"] = "المصاريف",
        ["menu.rapports"] = "التقارير",
        ["menu.parametres"] = "الإعدادات",

        // -------------------------------------------------------- Actions
        ["action.ajouter"] = "إضافة",
        ["action.modifier"] = "تعديل",
        ["action.supprimer"] = "حذف",
        ["action.enregistrer"] = "حفظ",
        ["action.annuler"] = "إلغاء",
        ["action.rechercher"] = "بحث",
        ["action.filtrer"] = "تصفية",
        ["action.exporter"] = "تصدير",
        ["action.imprimer"] = "طباعة",
        ["action.valider"] = "تأكيد",
        ["action.fermer"] = "إغلاق",
        ["action.actualiser"] = "تحديث",
        ["action.retour"] = "رجوع",
        ["action.connexion"] = "تسجيل الدخول",
        ["action.deconnexion"] = "تسجيل الخروج",

        // --------------------------------------------------------- États
        ["etat.chargement"] = "جارٍ التحميل…",
        ["etat.aucunResultat"] = "لا توجد نتائج",
        ["etat.aucuneDonnee"] = "لا توجد بيانات حالياً",
        ["etat.obligatoire"] = "حقل إجباري",

        // ------------------------------------------------------ Messages
        ["message.succes"] = "تمت العملية بنجاح",
        ["message.erreur"] = "خطأ",
        ["message.attention"] = "تنبيه",
        ["message.confirmation"] = "تأكيد",
        ["message.confirmerSuppression"] = "هل تريد فعلاً حذف هذا العنصر؟",
        ["message.stockInsuffisant"] = "المخزون غير كافٍ",
        ["message.enregistre"] = "تم الحفظ",
        ["message.erreurEnregistrement"] = "خطأ أثناء الحفظ",
        ["message.sessionAbsente"] = "لا توجد جلسة",

        // ------------------------------------------------------- Communs
        ["commun.nom"] = "الاسم",
        ["commun.reference"] = "المرجع",
        ["commun.categorie"] = "الفئة",
        ["commun.quantite"] = "الكمية",
        ["commun.prix"] = "السعر",
        ["commun.total"] = "المجموع",
        ["commun.date"] = "التاريخ",
        ["commun.statut"] = "الحالة",
        ["commun.client"] = "الزبون",
        ["commun.fournisseur"] = "المورّد",
        ["commun.produit"] = "المنتج",
        ["commun.matiere"] = "المادة",
        ["commun.stock"] = "المخزون",
        ["commun.description"] = "الوصف",
        ["commun.notes"] = "ملاحظات",
        ["commun.utilisateur"] = "المستخدم",
        ["commun.role"] = "الدور",
        ["commun.langue"] = "اللغة",

        // ------------------------------------------------------ Connexion
        ["connexion.titre"] = "تسجيل الدخول",
        ["connexion.nomUtilisateur"] = "اسم المستخدم",
        ["connexion.motDePasse"] = "كلمة المرور",
        ["connexion.echec"] = "اسم المستخدم أو كلمة المرور غير صحيحة.",
        ["connexion.compteBloque"] = "تم قفل الحساب بعد عدة محاولات. أعد المحاولة لاحقاً.",
        ["connexion.compteDesactive"] = "هذا الحساب معطّل.",

        // -------------------------------------------------- Application
        ["app.nom"] = "سيرامي برو",
        ["app.sousTitre"] = "إدارة الورشة"
    };

    /// <summary>Dictionnaire correspondant à une langue.</summary>
    public static IReadOnlyDictionary<string, string> Pour(Langue langue) => langue switch
    {
        Langue.Arabe => Arabe,
        _ => Francais
    };
}
