using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddUniqueConstraintOnVerificationCode : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_UserEmailVerification_Code",
                table: "UserEmailVerification",
                column: "Code",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_UserEmailVerification_Code",
                table: "UserEmailVerification");
        }
    }
}
