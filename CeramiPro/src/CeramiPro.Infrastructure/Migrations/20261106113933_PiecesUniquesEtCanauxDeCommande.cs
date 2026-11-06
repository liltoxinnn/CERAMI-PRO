using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CeramiPro.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class PiecesUniquesEtCanauxDeCommande : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Sales",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<bool>(
                name: "IsUniquePiece",
                table: "Products",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "Products",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<int>(
                name: "Channel",
                table: "CustomOrders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<uint>(
                name: "xmin",
                table: "CustomOrders",
                type: "xid",
                rowVersion: true,
                nullable: false,
                defaultValue: 0u);

            migrationBuilder.AddColumn<string>(
                name: "Instagram",
                table: "Customers",
                type: "character varying(100)",
                maxLength: 100,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Sales");

            migrationBuilder.DropColumn(
                name: "IsUniquePiece",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "xmin",
                table: "CustomOrders");

            migrationBuilder.DropColumn(
                name: "Instagram",
                table: "Customers");
        }
    }
}
