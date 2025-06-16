using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddUserMultifactorChallengeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequireTwoFactorAuthentication",
                schema: "crypter",
                table: "User",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "UserMultiFactorChallenge",
                schema: "crypter",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    VerificationCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMultiFactorChallenge", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserMultiFactorChallenge_User_Owner",
                        column: x => x.Owner,
                        principalSchema: "crypter",
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserMultiFactorChallenge_Owner",
                schema: "crypter",
                table: "UserMultiFactorChallenge",
                column: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserMultiFactorChallenge",
                schema: "crypter");

            migrationBuilder.DropColumn(
                name: "RequireTwoFactorAuthentication",
                schema: "crypter",
                table: "User");
        }
    }
}
