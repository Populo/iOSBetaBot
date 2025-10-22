using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace iOSBot.Data.Migrations
{
    /// <inheritdoc />
    public partial class mariatomssql : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Configs",
                columns: table => new
                {
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Configs", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "ErrorServers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorServers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Forums",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Track = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Forums", x => x.id);
                });

            migrationBuilder.CreateTable(
                name: "Servers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Track = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TagId = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Servers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Threads",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ServerId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    ChannelId = table.Column<decimal>(type: "decimal(20,0)", nullable: false),
                    Track = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Threads", x => x.id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Configs");

            migrationBuilder.DropTable(
                name: "ErrorServers");

            migrationBuilder.DropTable(
                name: "Forums");

            migrationBuilder.DropTable(
                name: "Servers");

            migrationBuilder.DropTable(
                name: "Threads");
        }
    }
}
