using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Crypter.Core.Migrations
{
   /// <inheritdoc />
   public partial class AddUserRecovery : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Timestamp",
                table: "UserConsent",
                newName: "Created");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "UserConsent",
                type: "boolean",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "Deactivated",
                table: "UserConsent",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "UserRecovery",
                columns: table => new
                {
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserRecovery", x => x.Owner);
                    table.ForeignKey(
                        name: "FK_UserRecovery_User_Owner",
                        column: x => x.Owner,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserRecovery_Code",
                table: "UserRecovery",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserRecovery");

            migrationBuilder.DropColumn(
                name: "Active",
                table: "UserConsent");

            migrationBuilder.DropColumn(
                name: "Deactivated",
                table: "UserConsent");

            migrationBuilder.RenameColumn(
                name: "Created",
                table: "UserConsent",
                newName: "Timestamp");
        }
    }
}
