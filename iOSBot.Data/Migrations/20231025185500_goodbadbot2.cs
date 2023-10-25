using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOSBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class goodbadbot2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GoodBots",
                table: "GoodBots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BadBots",
                table: "BadBots");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "GoodBots");

            migrationBuilder.DropColumn(
                name: "Timestamp",
                table: "BadBots");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "GoodBots",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddColumn<Guid>(
                name: "Id",
                table: "BadBots",
                type: "char(36)",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                collation: "ascii_general_ci");

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoodBots",
                table: "GoodBots",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BadBots",
                table: "BadBots",
                column: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_GoodBots",
                table: "GoodBots");

            migrationBuilder.DropPrimaryKey(
                name: "PK_BadBots",
                table: "BadBots");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "GoodBots");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "BadBots");

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "GoodBots",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "Timestamp",
                table: "BadBots",
                type: "datetime(6)",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddPrimaryKey(
                name: "PK_GoodBots",
                table: "GoodBots",
                column: "Timestamp");

            migrationBuilder.AddPrimaryKey(
                name: "PK_BadBots",
                table: "BadBots",
                column: "Timestamp");
        }
    }
}
