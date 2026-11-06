using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CeramiPro.Infrastructure.Migrations
{
    /// <summary>
    /// Passage du schéma complet (49 tables) au schéma simplifié (19 tables)
    /// sans rien perdre de ce que l'atelier a saisi.
    ///
    /// Trois principes :
    /// <list type="bullet">
    /// <item>les clients, produits, commandes, ventes, paiements et dépenses
    /// restent en place, avec leurs liens ;</item>
    /// <item>ce qui ne peut plus vivre dans le nouveau schéma — tables des
    /// modules retirés, colonnes supprimées — est déplacé dans le schéma
    /// « archives » de la même base, consultable à tout moment ;</item>
    /// <item>une table vide d'un module retiré est simplement supprimée : il
    /// n'y a rien à garder, et une installation neuve ne traîne pas d'archives
    /// inutiles.</item>
    /// </list>
    /// </summary>
    public partial class SimplificationDuSchema
    {
        /// <summary>Schéma qui reçoit les données des modules retirés.</summary>
        public const string SchemaArchives = "archives";

        /// <summary>
        /// Préparation faite avant toute modification de structure : tant
        /// que les anciennes tables existent, on peut encore y lire ce qu'il
        /// faut reporter dans les tables conservées.
        /// </summary>
        private static void PreserverLesDonnees(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql($"CREATE SCHEMA IF NOT EXISTS {SchemaArchives};");

            ConserverLesColonnesRetirees(migrationBuilder);
            AttribuerLesRoles(migrationBuilder);
            ReporterLesVariantes(migrationBuilder);
            RattacherLesPaiementsAuxVentes(migrationBuilder);
            NettoyerLesMouvementsDeStock(migrationBuilder);
        }

        /// <summary>
        /// Copie, dans les archives, les valeurs des colonnes qui vont
        /// disparaître des tables conservées.
        /// </summary>
        private static void ConserverLesColonnesRetirees(MigrationBuilder migrationBuilder)
        {
            // Rôle d'origine de chaque compte : deux rôles remplacent quatre,
            // l'ancien reste lisible. Les empreintes de mot de passe ne sont
            // volontairement pas recopiées.
            Instantane(migrationBuilder, "Users",
                """
                SELECT u."Id", u."UserName", u."FullName", u."RoleId",
                       r."Code" AS "CodeRole", r."Name" AS "NomRole"
                FROM public."Users" u
                LEFT JOIN public."Roles" r ON r."Id" = u."RoleId"
                """);

            Instantane(migrationBuilder, "Products",
                """
                SELECT "Id", "Reference", "Name", "Barcode", "QrCode"
                FROM public."Products"
                WHERE "Barcode" IS NOT NULL OR "QrCode" IS NOT NULL
                """);

            Instantane(migrationBuilder, "ProductImages",
                """
                SELECT "Id", "ProductId", "FilePath", "Kind"
                FROM public."ProductImages"
                """);

            Instantane(migrationBuilder, "Payments",
                """
                SELECT "Id", "PaymentNumber", "Direction", "InvoiceId"
                FROM public."Payments"
                WHERE "Direction" <> 0 OR "InvoiceId" IS NOT NULL
                """);

            Instantane(migrationBuilder, "SaleItems",
                """
                SELECT "Id", "SaleId", "ProductId", "ProductVariantId"
                FROM public."SaleItems"
                WHERE "ProductVariantId" IS NOT NULL
                """);

            Instantane(migrationBuilder, "BusinessSettings",
                """
                SELECT "Id", "InvoicePrefix", "PurchasePrefix", "ProductionPrefix", "FiringPrefix"
                FROM public."BusinessSettings"
                """);

            // L'historique des mouvements perd des colonnes et des lignes
            // (matières premières) : il est copié en entier.
            Instantane(migrationBuilder, "InventoryTransactions",
                """SELECT * FROM public."InventoryTransactions" """);
        }

        /// <summary>
        /// Deux rôles au lieu de quatre. Seuls les administrateurs le restent ;
        /// responsables, employés et caissiers deviennent employés — leur
        /// donner l'administration leur ouvrirait plus qu'ils n'avaient.
        /// Un administrateur peut ensuite promouvoir qui il veut.
        /// </summary>
        private static void AttribuerLesRoles(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Role",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql(
                """
                UPDATE public."Users" u
                SET "Role" = 1
                FROM public."Roles" r
                WHERE r."Id" = u."RoleId" AND r."Code" = 'administrateur';
                """);

            // Sans aucun administrateur, plus personne ne pourrait gérer les
            // comptes : le compte « admin », ou à défaut le plus ancien compte
            // actif, le devient.
            migrationBuilder.Sql(
                """
                UPDATE public."Users"
                SET "Role" = 1
                WHERE "Id" = (
                    SELECT "Id" FROM public."Users"
                    WHERE NOT EXISTS (SELECT 1 FROM public."Users" WHERE "Role" = 1)
                    ORDER BY ("UserName" = 'admin') DESC, "IsActive" DESC, "Id"
                    LIMIT 1);
                """);
        }

        /// <summary>
        /// Les variantes disparaissent : leur stock rejoint celui du produit,
        /// et leurs mouvements sont rattachés au produit avec le nom de la
        /// variante en note.
        /// </summary>
        private static void ReporterLesVariantes(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE public."Products" p
                SET "CurrentStock" = p."CurrentStock" + v."Stock"
                FROM (
                    SELECT "ProductId", SUM("CurrentStock") AS "Stock"
                    FROM public."ProductVariants"
                    GROUP BY "ProductId"
                ) v
                WHERE v."ProductId" = p."Id" AND v."Stock" <> 0;
                """);

            migrationBuilder.Sql(
                """
                UPDATE public."InventoryTransactions" t
                SET "ProductId" = COALESCE(t."ProductId", v."ProductId"),
                    "Notes" = LEFT(COALESCE(t."Notes" || ' — ', '') || 'Variante : ' || v."Name", 1000)
                FROM public."ProductVariants" v
                WHERE v."Id" = t."ProductVariantId";
                """);
        }

        /// <summary>
        /// Un paiement enregistré sur une facture garde le lien vers la vente
        /// ou la commande que cette facture concernait.
        /// </summary>
        private static void RattacherLesPaiementsAuxVentes(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE public."Payments" p
                SET "SaleId" = COALESCE(p."SaleId", i."SaleId"),
                    "CustomOrderId" = COALESCE(p."CustomOrderId", i."CustomOrderId"),
                    "CustomerId" = COALESCE(p."CustomerId", i."CustomerId")
                FROM public."Invoices" i
                WHERE i."Id" = p."InvoiceId";
                """);

            // Le logiciel n'a jamais écrit de décaissement dans cette table ;
            // si une ligne en contient malgré tout, elle ne doit pas être
            // comptée comme une recette. Elle est masquée, pas effacée.
            migrationBuilder.Sql(
                """
                UPDATE public."Payments"
                SET "IsDeleted" = TRUE,
                    "DeletedAt" = now(),
                    "DeletionReason" = 'Décaissement : module fournisseurs retiré lors de la simplification.'
                WHERE "Direction" = 1 AND NOT "IsDeleted";
                """);
        }

        /// <summary>
        /// Le stock ne suit plus que les produits. Les mouvements de matières
        /// premières quittent la table (ils restent dans les archives), et les
        /// types propres aux modules retirés reçoivent leur équivalent.
        /// </summary>
        private static void NettoyerLesMouvementsDeStock(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                UPDATE public."InventoryTransactions"
                SET "ReversedTransactionId" = NULL
                WHERE "ReversedTransactionId" IN (
                    SELECT "Id" FROM public."InventoryTransactions" WHERE "ProductId" IS NULL);

                DELETE FROM public."InventoryTransactions" WHERE "ProductId" IS NULL;
                """);

            // 1 = consommation en production, 2 = entrée de production,
            // 7 = transfert. Les autres numéros sont inchangés.
            migrationBuilder.Sql(
                """
                UPDATE public."InventoryTransactions"
                SET "Notes" = LEFT(COALESCE("Notes" || ' — ', '') ||
                        CASE "TransactionType"
                            WHEN 1 THEN 'Ancien type : consommation en production'
                            WHEN 2 THEN 'Ancien type : entrée de production'
                            ELSE 'Ancien type : transfert'
                        END, 1000),
                    "TransactionType" = CASE "TransactionType" WHEN 2 THEN 0 ELSE 5 END
                WHERE "TransactionType" IN (1, 2, 7);
                """);
        }

        /// <summary>
        /// Déplace une table d'un module retiré dans les archives si elle
        /// contient des données, et la supprime sinon.
        /// </summary>
        private static void ArchiverOuSupprimer(MigrationBuilder migrationBuilder, string table)
            => migrationBuilder.Sql(
                $$"""
                DO $$
                BEGIN
                    IF EXISTS (SELECT 1 FROM public."{{table}}") THEN
                        ALTER TABLE public."{{table}}" SET SCHEMA {{SchemaArchives}};
                    ELSE
                        DROP TABLE public."{{table}}" CASCADE;
                    END IF;
                END $$;
                """);

        /// <summary>
        /// Recopie le résultat d'une requête dans « archives.&lt;table&gt;_avant_simplification »,
        /// seulement s'il y a quelque chose à garder.
        /// </summary>
        private static void Instantane(MigrationBuilder migrationBuilder, string table, string requete)
            => migrationBuilder.Sql(
                $$"""
                DO $$
                BEGIN
                    IF EXISTS ({{requete}}) THEN
                        CREATE TABLE {{SchemaArchives}}."{{table}}_avant_simplification" AS {{requete}};
                    END IF;
                END $$;
                """);

        /// <summary>
        /// Les archives se consultent ; elles ne doivent rien empêcher. Leurs
        /// clés étrangères vers les tables vivantes sont retirées : sans cela,
        /// supprimer un client ou restaurer une sauvegarde buterait sur une
        /// ancienne facture archivée.
        /// </summary>
        private static void RendreLesArchivesPassives(MigrationBuilder migrationBuilder)
            => migrationBuilder.Sql(
                $$"""
                DO $$
                DECLARE
                    lien record;
                BEGIN
                    FOR lien IN
                        SELECT contrainte.conname AS nom, source.relname AS table_source
                        FROM pg_constraint contrainte
                        JOIN pg_class source ON source.oid = contrainte.conrelid
                        JOIN pg_namespace ns_source ON ns_source.oid = source.relnamespace
                        JOIN pg_class cible ON cible.oid = contrainte.confrelid
                        JOIN pg_namespace ns_cible ON ns_cible.oid = cible.relnamespace
                        WHERE contrainte.contype = 'f'
                          AND ns_source.nspname = '{{SchemaArchives}}'
                          AND ns_cible.nspname <> '{{SchemaArchives}}'
                    LOOP
                        EXECUTE format('ALTER TABLE {{SchemaArchives}}.%I DROP CONSTRAINT %I',
                            lien.table_source, lien.nom);
                    END LOOP;

                    IF NOT EXISTS (SELECT 1 FROM pg_tables WHERE schemaname = '{{SchemaArchives}}') THEN
                        DROP SCHEMA {{SchemaArchives}};
                    ELSE
                        EXECUTE 'COMMENT ON SCHEMA {{SchemaArchives}} IS '
                            || quote_literal('Données des modules retirés lors de la simplification de CeramiPro. '
                                || 'Consultation seule : le logiciel ne lit ni n''écrit dans ce schéma.');
                    END IF;
                END $$;
                """);
    }
}
