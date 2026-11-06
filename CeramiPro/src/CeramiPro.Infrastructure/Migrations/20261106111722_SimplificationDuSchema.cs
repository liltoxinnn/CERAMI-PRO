using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace CeramiPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class SimplificationDuSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Tout ce qui suit s'exécute dans une seule transaction : si une
            // étape échoue, la base reste exactement dans son état d'origine.
            PreserverLesDonnees(migrationBuilder);

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_MaterialBatches_MaterialBatchId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Materials_MaterialId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_ProductVariants_ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_ProductionOrders_ProductionOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_Purchases_PurchaseId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_InventoryTransactions_StockAdjustments_StockAdjustmentId",
                table: "InventoryTransactions");

            migrationBuilder.DropForeignKey(
                name: "FK_Payments_Invoices_InvoiceId",
                table: "Payments");

            migrationBuilder.DropForeignKey(
                name: "FK_SaleItems_ProductVariants_ProductVariantId",
                table: "SaleItems");

            migrationBuilder.DropForeignKey(
                name: "FK_Users_Roles_RoleId",
                table: "Users");

            ArchiverOuSupprimer(migrationBuilder, "DecorationImages");

            ArchiverOuSupprimer(migrationBuilder, "FiringBatchItems");

            ArchiverOuSupprimer(migrationBuilder, "InvoiceItems");

            ArchiverOuSupprimer(migrationBuilder, "MaterialBatches");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "NotificationSettings");

            ArchiverOuSupprimer(migrationBuilder, "ProductionMaterials");

            ArchiverOuSupprimer(migrationBuilder, "ProductionStageHistory");

            ArchiverOuSupprimer(migrationBuilder, "ProductRecipeItems");

            ArchiverOuSupprimer(migrationBuilder, "ProductVariants");

            ArchiverOuSupprimer(migrationBuilder, "QualityIssues");

            migrationBuilder.DropTable(
                name: "RolePermissions");

            ArchiverOuSupprimer(migrationBuilder, "StockAdjustments");

            ArchiverOuSupprimer(migrationBuilder, "SupplierPayments");

            ArchiverOuSupprimer(migrationBuilder, "DecorationOrders");

            ArchiverOuSupprimer(migrationBuilder, "Invoices");

            ArchiverOuSupprimer(migrationBuilder, "PurchaseItems");

            ArchiverOuSupprimer(migrationBuilder, "QualityChecks");

            migrationBuilder.DropTable(
                name: "Permissions");

            migrationBuilder.DropTable(
                name: "Roles");

            ArchiverOuSupprimer(migrationBuilder, "DecorationTypes");

            ArchiverOuSupprimer(migrationBuilder, "Materials");

            ArchiverOuSupprimer(migrationBuilder, "Purchases");

            ArchiverOuSupprimer(migrationBuilder, "FiringBatches");

            ArchiverOuSupprimer(migrationBuilder, "ProductionOrders");

            ArchiverOuSupprimer(migrationBuilder, "MaterialCategories");

            ArchiverOuSupprimer(migrationBuilder, "Units");

            ArchiverOuSupprimer(migrationBuilder, "Suppliers");

            ArchiverOuSupprimer(migrationBuilder, "Kilns");

            ArchiverOuSupprimer(migrationBuilder, "ProductRecipes");

            migrationBuilder.DropIndex(
                name: "IX_Users_RefreshToken",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_Users_RoleId",
                table: "Users");

            migrationBuilder.DropIndex(
                name: "IX_SaleItems_ProductVariantId",
                table: "SaleItems");

            migrationBuilder.DropIndex(
                name: "IX_Products_Barcode",
                table: "Products");

            migrationBuilder.DropIndex(
                name: "IX_Payments_InvoiceId",
                table: "Payments");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ItemType_MaterialId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ItemType_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_MaterialBatchId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_MaterialId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProductId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProductionOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_PurchaseId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "RefreshToken",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RefreshTokenExpiresAt",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "RoleId",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "SaleItems");

            migrationBuilder.DropColumn(
                name: "Barcode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "QrCode",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Kind",
                table: "ProductImages");

            migrationBuilder.DropColumn(
                name: "Direction",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "InvoiceId",
                table: "Payments");

            migrationBuilder.DropColumn(
                name: "ItemType",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "MaterialBatchId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "MaterialId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ProductVariantId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "ProductionOrderId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "PurchaseId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "FiringPrefix",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "InvoicePrefix",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "ProductionPrefix",
                table: "BusinessSettings");

            migrationBuilder.DropColumn(
                name: "PurchasePrefix",
                table: "BusinessSettings");

            // L'ancien lien vers un ajustement de stock n'a rien à voir avec une
            // commande : la colonne est retirée et la nouvelle part vide.
            migrationBuilder.DropIndex(
                name: "IX_InventoryTransactions_StockAdjustmentId",
                table: "InventoryTransactions");

            migrationBuilder.DropColumn(
                name: "StockAdjustmentId",
                table: "InventoryTransactions");

            migrationBuilder.AddColumn<int>(
                name: "CustomOrderId",
                table: "InventoryTransactions",
                type: "integer",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CustomOrderId",
                table: "InventoryTransactions",
                column: "CustomOrderId");

            migrationBuilder.RenameColumn(
                name: "InvoiceFooter",
                table: "BusinessSettings",
                newName: "ReceiptFooter");

            migrationBuilder.AddColumn<bool>(
                name: "IsSystem",
                table: "ProductCategories",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<string>(
                name: "Reference",
                table: "InventoryTransactions",
                type: "character varying(200)",
                maxLength: 200,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "character varying(60)",
                oldMaxLength: 60,
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "ProductId",
                table: "InventoryTransactions",
                type: "integer",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPrefix",
                table: "BusinessSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "CLI");

            migrationBuilder.AddColumn<string>(
                name: "ExpensePrefix",
                table: "BusinessSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "DEP");

            migrationBuilder.AddColumn<string>(
                name: "ProductPrefix",
                table: "BusinessSettings",
                type: "character varying(10)",
                maxLength: 10,
                nullable: false,
                defaultValue: "PRD");

            migrationBuilder.CreateIndex(
                name: "IX_Sales_Status",
                table: "Sales",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_ProductId_OccurredAt",
                table: "InventoryTransactions",
                columns: new[] { "ProductId", "OccurredAt" });

            migrationBuilder.AddForeignKey(
                name: "FK_InventoryTransactions_CustomOrders_CustomOrderId",
                table: "InventoryTransactions",
                column: "CustomOrderId",
                principalTable: "CustomOrders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            RendreLesArchivesPassives(migrationBuilder);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Revenir en arrière recréerait des tables vides et des colonnes
            // sans leurs valeurs : ce serait perdre des données en silence.
            // Les données retirées sont conservées dans le schéma « archives »,
            // et l'ancien état complet se retrouve en restaurant la sauvegarde
            // faite avant la mise à jour.
            throw new NotSupportedException(
                "La simplification du schéma ne peut pas être annulée par une migration. " +
                "Restaurez la sauvegarde réalisée avant la mise à jour ; les données des " +
                "modules retirés restent consultables dans le schéma « archives ».");
        }
    }
}
