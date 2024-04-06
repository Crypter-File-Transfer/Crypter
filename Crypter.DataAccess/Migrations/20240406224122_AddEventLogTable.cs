using System;
using System.Text.Json;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddEventLogTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EventLog",
                schema: "crypter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventLogType = table.Column<int>(type: "integer", nullable: false),
                    AdditionalData = table.Column<JsonDocument>(type: "jsonb", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EventLog", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EventLog_EventLogType",
                schema: "crypter",
                table: "EventLog",
                column: "EventLogType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EventLog",
                schema: "crypter");
        }
    }
}
