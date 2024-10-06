using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddFileTransferPartsColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Parts",
                schema: "crypter",
                table: "UserFileTransfer",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Parts",
                schema: "crypter",
                table: "AnonymousFileTransfer",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Parts",
                schema: "crypter",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "Parts",
                schema: "crypter",
                table: "AnonymousFileTransfer");
        }
    }
}
