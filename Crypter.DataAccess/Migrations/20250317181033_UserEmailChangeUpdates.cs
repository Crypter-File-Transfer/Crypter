using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UserEmailChangeUpdates : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmailVerification",
                schema: "crypter");

            migrationBuilder.DropColumn(
                name: "EmailVerified",
                schema: "crypter",
                table: "User");

            migrationBuilder.CreateTable(
                name: "UserEmailChange",
                schema: "crypter",
                columns: table => new
                {
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    EmailAddress = table.Column<string>(type: "citext", nullable: false),
                    Code = table.Column<Guid>(type: "uuid", nullable: true),
                    VerificationKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    VerificationSent = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailChange", x => x.Owner);
                    table.ForeignKey(
                        name: "FK_UserEmailChange_User_Owner",
                        column: x => x.Owner,
                        principalSchema: "crypter",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailChange_Code",
                schema: "crypter",
                table: "UserEmailChange",
                column: "Code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailChange_EmailAddress",
                schema: "crypter",
                table: "UserEmailChange",
                column: "EmailAddress",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserEmailChange",
                schema: "crypter");

            migrationBuilder.AddColumn<bool>(
                name: "EmailVerified",
                schema: "crypter",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserEmailVerification",
                schema: "crypter",
                columns: table => new
                {
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    Code = table.Column<Guid>(type: "uuid", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    VerificationKey = table.Column<byte[]>(type: "bytea", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserEmailVerification", x => x.Owner);
                    table.ForeignKey(
                        name: "FK_UserEmailVerification_User_Owner",
                        column: x => x.Owner,
                        principalSchema: "crypter",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserEmailVerification_Code",
                schema: "crypter",
                table: "UserEmailVerification",
                column: "Code",
                unique: true);
        }
    }
}
