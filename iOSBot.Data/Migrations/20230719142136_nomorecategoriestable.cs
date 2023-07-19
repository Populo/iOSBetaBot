using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOSBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class nomorecategoriestable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Servers_Categories_CategoryId",
                table: "Servers");

            migrationBuilder.DropForeignKey(
                name: "FK_Updates_Categories_CategoryId",
                table: "Updates");

            migrationBuilder.DropTable(
                name: "Categories");

            migrationBuilder.DropIndex(
                name: "IX_Updates_CategoryId",
                table: "Updates");

            migrationBuilder.DropIndex(
                name: "IX_Servers_CategoryId",
                table: "Servers");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "CategoryId",
                table: "Servers");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Updates",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Category",
                table: "Servers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Category",
                table: "Updates");

            migrationBuilder.DropColumn(
                name: "Category",
                table: "Servers");

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "Updates",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "CategoryId",
                table: "Servers",
                type: "varchar(255)",
                nullable: false,
                defaultValue: "")
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Categories",
                columns: table => new
                {
                    Name = table.Column<string>(type: "varchar(255)", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Categories", x => x.Name);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Updates_CategoryId",
                table: "Updates",
                column: "CategoryId");

            migrationBuilder.CreateIndex(
                name: "IX_Servers_CategoryId",
                table: "Servers",
                column: "CategoryId");

            migrationBuilder.AddForeignKey(
                name: "FK_Servers_Categories_CategoryId",
                table: "Servers",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Updates_Categories_CategoryId",
                table: "Updates",
                column: "CategoryId",
                principalTable: "Categories",
                principalColumn: "Name",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
