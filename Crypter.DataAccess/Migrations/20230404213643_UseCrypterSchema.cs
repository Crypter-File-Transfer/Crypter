using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class UseCrypterSchema : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "crypter");

            migrationBuilder.RenameTable(
                name: "UserToken",
                newName: "UserToken",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserRecovery",
                newName: "UserRecovery",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserProfile",
                newName: "UserProfile",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserPrivacySetting",
                newName: "UserPrivacySetting",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserNotificationSetting",
                newName: "UserNotificationSetting",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserMessageTransfer",
                newName: "UserMessageTransfer",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserMasterKey",
                newName: "UserMasterKey",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserKeyPair",
                newName: "UserKeyPair",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserFileTransfer",
                newName: "UserFileTransfer",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserFailedLogin",
                newName: "UserFailedLogin",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserEmailVerification",
                newName: "UserEmailVerification",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserContact",
                newName: "UserContact",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "UserConsent",
                newName: "UserConsent",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "User",
                newName: "User",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "AnonymousMessageTransfer",
                newName: "AnonymousMessageTransfer",
                newSchema: "crypter");

            migrationBuilder.RenameTable(
                name: "AnonymousFileTransfer",
                newName: "AnonymousFileTransfer",
                newSchema: "crypter");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameTable(
                name: "UserToken",
                schema: "crypter",
                newName: "UserToken");

            migrationBuilder.RenameTable(
                name: "UserRecovery",
                schema: "crypter",
                newName: "UserRecovery");

            migrationBuilder.RenameTable(
                name: "UserProfile",
                schema: "crypter",
                newName: "UserProfile");

            migrationBuilder.RenameTable(
                name: "UserPrivacySetting",
                schema: "crypter",
                newName: "UserPrivacySetting");

            migrationBuilder.RenameTable(
                name: "UserNotificationSetting",
                schema: "crypter",
                newName: "UserNotificationSetting");

            migrationBuilder.RenameTable(
                name: "UserMessageTransfer",
                schema: "crypter",
                newName: "UserMessageTransfer");

            migrationBuilder.RenameTable(
                name: "UserMasterKey",
                schema: "crypter",
                newName: "UserMasterKey");

            migrationBuilder.RenameTable(
                name: "UserKeyPair",
                schema: "crypter",
                newName: "UserKeyPair");

            migrationBuilder.RenameTable(
                name: "UserFileTransfer",
                schema: "crypter",
                newName: "UserFileTransfer");

            migrationBuilder.RenameTable(
                name: "UserFailedLogin",
                schema: "crypter",
                newName: "UserFailedLogin");

            migrationBuilder.RenameTable(
                name: "UserEmailVerification",
                schema: "crypter",
                newName: "UserEmailVerification");

            migrationBuilder.RenameTable(
                name: "UserContact",
                schema: "crypter",
                newName: "UserContact");

            migrationBuilder.RenameTable(
                name: "UserConsent",
                schema: "crypter",
                newName: "UserConsent");

            migrationBuilder.RenameTable(
                name: "User",
                schema: "crypter",
                newName: "User");

            migrationBuilder.RenameTable(
                name: "AnonymousMessageTransfer",
                schema: "crypter",
                newName: "AnonymousMessageTransfer");

            migrationBuilder.RenameTable(
                name: "AnonymousFileTransfer",
                schema: "crypter",
                newName: "AnonymousFileTransfer");
        }
    }
}
