using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CeramicWorkshop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class IndexUniqueAlertes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Une base déjà en service peut contenir des alertes en double,
            // créées avant l'ajout de cet index. On ne garde que la plus
            // ancienne de chaque groupe avant de poser la contrainte.
            migrationBuilder.Sql("""
                DELETE FROM "Notifications" a
                USING "Notifications" b
                WHERE a."Id" > b."Id"
                  AND a."Type" = b."Type"
                  AND a."EntityName" IS NOT DISTINCT FROM b."EntityName"
                  AND a."EntityId" IS NOT DISTINCT FROM b."EntityId"
                  AND a."EntityName" IS NOT NULL;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_Type_Entite",
                table: "Notifications",
                columns: new[] { "Type", "EntityName", "EntityId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Notifications_Type_Entite",
                table: "Notifications");
        }
    }
}
