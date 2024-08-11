using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOSBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class enableddevices : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Enabled",
                table: "Devices",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "Devices",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Enabled",
                table: "Devices");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "Devices");
        }
    }
}
