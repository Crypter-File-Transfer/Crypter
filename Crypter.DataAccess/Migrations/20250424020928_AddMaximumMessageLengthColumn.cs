using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddMaximumMessageLengthColumn : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "MaximumMessageLength",
                schema: "crypter",
                table: "TransferTier",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.UpdateData(
                schema: "crypter",
                table: "TransferTier",
                keyColumn: "Id",
                keyValues: [1, 2, 3],
                column: "MaximumMessageLength",
                values: [1024, 4096, 4096]);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "MaximumMessageLength",
                schema: "crypter",
                table: "TransferTier");
        }
    }
}
