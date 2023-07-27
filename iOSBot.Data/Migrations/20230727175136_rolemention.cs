using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOSBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class rolemention : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TagId",
                table: "Servers",
                type: "longtext",
                nullable: false)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TagId",
                table: "Servers");
        }
    }
}
