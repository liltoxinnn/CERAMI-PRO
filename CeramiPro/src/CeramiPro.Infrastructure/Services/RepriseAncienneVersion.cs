using System.Text;

namespace CeramiPro.Infrastructure.Services;

/// <summary>
/// Reprise d'une sauvegarde faite avec l'ancienne version de CeramiPro,
/// avant la simplification du schéma (49 tables).
///
/// Les fichiers de la sauvegarde sont d'abord chargés tels quels dans des
/// tables temporaires, puis recopiés dans les tables actuelles en appliquant
/// les mêmes conversions que la migration « SimplificationDuSchema » : deux
/// rôles au lieu de quatre, quatre étapes de commande au lieu de dix, stock
/// des variantes reporté sur le produit, mouvements de matières premières
/// écartés, paiements rattachés à la vente de leur facture. Les tables des modules retirés ne sont pas reprises —
/// elles restent dans le fichier de sauvegarde, qui n'est jamais modifié.
/// </summary>
public static class RepriseAncienneVersion
{
    /// <summary>Tables qui existaient dans l'ancienne version et ont été retirées.</summary>
    public static readonly IReadOnlySet<string> TablesRetirees = new HashSet<string>(StringComparer.Ordinal)
    {
        "DecorationImages", "DecorationOrders", "DecorationTypes",
        "FiringBatchItems", "FiringBatches", "Kilns",
        "InvoiceItems", "Invoices",
        "MaterialBatches", "MaterialCategories", "Materials", "Units",
        "Notifications", "NotificationSettings",
        "Permissions", "RolePermissions", "Roles",
        "ProductionMaterials", "ProductionOrders", "ProductionStageHistory",
        "ProductRecipeItems", "ProductRecipes", "ProductVariants",
        "PurchaseItems", "Purchases", "SupplierPayments", "Suppliers",
        "QualityChecks", "QualityIssues",
        "StockAdjustments"
    };

    /// <summary>
    /// L'ancienne version écrivait toutes ses tables dans chaque sauvegarde :
    /// la présence d'une seule table retirée suffit à la reconnaître.
    /// </summary>
    public static bool EstAncienFormat(IEnumerable<string> tablesDuFichier)
        => tablesDuFichier.Any(TablesRetirees.Contains);

    /// <summary>Nom de la table temporaire qui reçoit le fichier d'une table.</summary>
    public static string TableTemporaire(string table) => $"ancien_{table}";

    /// <summary>
    /// Tables retirées dont la conversion a besoin. Si la sauvegarde ne les
    /// contient pas, elles sont créées vides pour que les requêtes restent
    /// valides.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string[]> TablesDeReference =
        new Dictionary<string, string[]>(StringComparer.Ordinal)
        {
            ["Roles"] = new[] { "Id", "Code" },
            ["ProductVariants"] = new[] { "Id", "ProductId", "Name", "CurrentStock" },
            ["Invoices"] = new[] { "Id", "SaleId", "CustomOrderId", "CustomerId" }
        };

    /// <summary>
    /// Requête qui recopie une table de la sauvegarde dans la table actuelle.
    /// </summary>
    /// <param name="table">Table actuelle.</param>
    /// <param name="colonnesCibles">Colonnes de la table actuelle et leur type SQL.</param>
    /// <param name="colonnesAnciennes">Colonnes présentes dans le fichier de la sauvegarde.</param>
    public static string RequeteDeReprise(
        string table,
        IReadOnlyList<(string Nom, string Type)> colonnesCibles,
        IReadOnlySet<string> colonnesAnciennes)
    {
        var conversions = Conversions(table, colonnesAnciennes);
        var noms = new List<string>();
        var valeurs = new List<string>();

        foreach (var (nom, type) in colonnesCibles)
        {
            if (conversions.Colonnes.TryGetValue(nom, out var expression))
            {
                noms.Add($"\"{nom}\"");
                valeurs.Add($"({expression})::{type}");
            }
            else if (colonnesAnciennes.Contains(nom))
            {
                noms.Add($"\"{nom}\"");
                valeurs.Add($"a.\"{nom}\"::{type}");
            }

            // Une colonne apparue depuis reçoit sa valeur par défaut.
        }

        var sql = new StringBuilder()
            .Append($"INSERT INTO \"{table}\" ({string.Join(", ", noms)})\n")
            .Append($"SELECT {string.Join(", ", valeurs)}\n")
            .Append($"FROM \"{TableTemporaire(table)}\" a");

        if (conversions.Jointure is not null)
        {
            sql.Append('\n').Append(conversions.Jointure);
        }

        if (conversions.Filtre is not null)
        {
            sql.Append("\nWHERE ").Append(conversions.Filtre);
        }

        return sql.ToString();
    }

    /// <summary>
    /// Après la reprise des comptes : sans aucun administrateur, plus
    /// personne ne pourrait gérer les comptes. Même règle que la migration.
    /// </summary>
    public const string GarantirUnAdministrateur = """
        UPDATE "Users"
        SET "Role" = 1
        WHERE "Id" = (
            SELECT "Id" FROM "Users"
            WHERE NOT EXISTS (SELECT 1 FROM "Users" WHERE "Role" = 1)
            ORDER BY ("UserName" = 'admin') DESC, "IsActive" DESC, "Id"
            LIMIT 1)
        """;

    private sealed record Conversion(
        IReadOnlyDictionary<string, string> Colonnes,
        string? Jointure = null,
        string? Filtre = null);

    private static readonly Conversion Aucune = new(new Dictionary<string, string>());

    /// <summary>
    /// Les conversions propres à chaque table. Une conversion n'est appliquée
    /// que si la sauvegarde contient les colonnes dont elle a besoin.
    /// </summary>
    private static Conversion Conversions(string table, IReadOnlySet<string> anciennes)
    {
        bool Possede(params string[] colonnes) => colonnes.All(anciennes.Contains);

        switch (table)
        {
            case "Users" when Possede("RoleId"):
                return new Conversion(new Dictionary<string, string>
                {
                    // Seuls les administrateurs le restent ; les autres
                    // rôles deviennent « Employé ».
                    ["Role"] = """
                        CASE WHEN EXISTS (
                            SELECT 1 FROM "ancien_Roles" r
                            WHERE r."Id" = a."RoleId" AND r."Code" = 'administrateur')
                        THEN 1 ELSE 0 END
                        """
                });

            case "Products" when Possede("Id", "CurrentStock"):
                return new Conversion(new Dictionary<string, string>
                {
                    ["CurrentStock"] = """
                        a."CurrentStock"::numeric + COALESCE((
                            SELECT SUM(v."CurrentStock"::numeric) FROM "ancien_ProductVariants" v
                            WHERE v."ProductId" = a."Id"), 0)
                        """
                });

            case "InventoryTransactions" when Possede("ProductId", "ProductVariantId", "TransactionType"):
                return new Conversion(
                    new Dictionary<string, string>
                    {
                        ["ProductId"] = """COALESCE(a."ProductId", v."ProductId")""",

                        // 1 = consommation en production, 2 = entrée de
                        // production, 7 = transfert ; les autres numéros
                        // sont inchangés.
                        ["TransactionType"] = """
                            CASE a."TransactionType" WHEN '1' THEN '5' WHEN '2' THEN '0' WHEN '7' THEN '5'
                            ELSE a."TransactionType" END
                            """,

                        ["Notes"] = """
                            CASE WHEN v."Name" IS NULL AND a."TransactionType" NOT IN ('1', '2', '7')
                            THEN a."Notes"
                            ELSE LEFT(CONCAT_WS(' — ', a."Notes",
                                'Variante : ' || v."Name",
                                CASE a."TransactionType"
                                    WHEN '1' THEN 'Ancien type : consommation en production'
                                    WHEN '2' THEN 'Ancien type : entrée de production'
                                    WHEN '7' THEN 'Ancien type : transfert'
                                END), 1000)
                            END
                            """,

                        // Un mouvement qui inversait un mouvement de matière
                        // ne pointe plus vers rien.
                        ["ReversedTransactionId"] = """
                            CASE WHEN EXISTS (
                                SELECT 1 FROM "ancien_InventoryTransactions" x
                                LEFT JOIN "ancien_ProductVariants" xv ON xv."Id" = x."ProductVariantId"
                                WHERE x."Id" = a."ReversedTransactionId"
                                  AND COALESCE(x."ProductId", xv."ProductId") IS NULL)
                            THEN NULL ELSE a."ReversedTransactionId" END
                            """
                    },
                    Jointure: """LEFT JOIN "ancien_ProductVariants" v ON v."Id" = a."ProductVariantId" """,
                    // Les mouvements de matières premières ne sont pas repris.
                    Filtre: """COALESCE(a."ProductId", v."ProductId") IS NOT NULL""");

            case "Payments" when Possede("InvoiceId", "Direction", "IsDeleted"):
                const string decaissement = """COALESCE(a."Direction", '0') = '1' AND NOT a."IsDeleted"::boolean""";
                return new Conversion(
                    new Dictionary<string, string>
                    {
                        ["SaleId"] = """COALESCE(a."SaleId", i."SaleId")""",
                        ["CustomOrderId"] = """COALESCE(a."CustomOrderId", i."CustomOrderId")""",
                        ["CustomerId"] = """COALESCE(a."CustomerId", i."CustomerId")""",

                        // Un décaissement ne doit jamais compter comme une
                        // recette : il est masqué, pas effacé.
                        ["IsDeleted"] = $"""a."IsDeleted"::boolean OR ({decaissement})""",
                        ["DeletedAt"] = $"""CASE WHEN {decaissement} THEN now()::text ELSE a."DeletedAt" END""",
                        ["DeletionReason"] = $"""
                            CASE WHEN {decaissement}
                            THEN 'Décaissement : module fournisseurs retiré lors de la simplification.'
                            ELSE a."DeletionReason" END
                            """
                    },
                    Jointure: """LEFT JOIN "ancien_Invoices" i ON i."Id" = a."InvoiceId" """);

            case "CustomOrders" when Possede("Status"):
                // Quatre étapes au lieu de dix, comme dans la migration.
                const string ancienneEtape = """
                    CASE a."Status"
                        WHEN '1' THEN 'conception'
                        WHEN '2' THEN 'validation client'
                        WHEN '4' THEN 'cuisson'
                        WHEN '5' THEN 'décoration'
                        WHEN '6' THEN 'contrôle qualité'
                    END
                    """;
                return new Conversion(new Dictionary<string, string>
                {
                    ["Status"] = """
                        CASE WHEN a."Status" IN ('1', '2') THEN '0'
                             WHEN a."Status" IN ('4', '5', '6') THEN '3'
                             ELSE a."Status" END
                        """,
                    ["Notes"] = $"""
                        CASE WHEN a."Status" IN ('1', '2', '4', '5', '6')
                        THEN LEFT(CONCAT_WS(' — ', a."Notes", 'Ancienne étape : ' || {ancienneEtape}), 2000)
                        ELSE a."Notes" END
                        """
                });

            case "BusinessSettings" when Possede("InvoiceFooter"):
                return new Conversion(new Dictionary<string, string>
                {
                    ["ReceiptFooter"] = """a."InvoiceFooter" """
                });

            default:
                return Aucune;
        }
    }
}
