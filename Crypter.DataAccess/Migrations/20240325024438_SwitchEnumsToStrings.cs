using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class SwitchEnumsToStrings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Type",
                schema: "crypter",
                table: "UserToken",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");
            
            migrationBuilder.Sql(
                "UPDATE crypter.\"UserToken\" SET \"Type\" = 'Authentication' WHERE \"Type\" = '0';" +
                "UPDATE crypter.\"UserToken\" SET \"Type\" = 'Session' WHERE \"Type\" = '1';" +
                "UPDATE crypter.\"UserToken\" SET \"Type\" = 'Device' WHERE \"Type\" = '2';");
            
            migrationBuilder.AlterColumn<string>(
                name: "Visibility",
                schema: "crypter",
                table: "UserPrivacySetting",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = 'None' WHERE \"Visibility\" = '0';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = 'Contacts' WHERE \"Visibility\" = '1';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = 'Authenticated' WHERE \"Visibility\" = '2';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = 'Everyone' WHERE \"Visibility\" = '3';");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiveMessages",
                schema: "crypter",
                table: "UserPrivacySetting",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = 'None' WHERE \"ReceiveMessages\" = '0';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = 'ExchangedKeys' WHERE \"ReceiveMessages\" = '1';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = 'Contacts' WHERE \"ReceiveMessages\" = '2';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = 'Authenticated' WHERE \"ReceiveMessages\" = '3';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = 'Everyone' WHERE \"ReceiveMessages\" = '4';");

            migrationBuilder.AlterColumn<string>(
                name: "ReceiveFiles",
                schema: "crypter",
                table: "UserPrivacySetting",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = 'None' WHERE \"ReceiveFiles\" = '0';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = 'ExchangedKeys' WHERE \"ReceiveFiles\" = '1';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = 'Contacts' WHERE \"ReceiveFiles\" = '2';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = 'Authenticated' WHERE \"ReceiveFiles\" = '3';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = 'Everyone' WHERE \"ReceiveFiles\" = '4';");

            migrationBuilder.AlterColumn<string>(
                name: "ConsentType",
                schema: "crypter",
                table: "UserConsent",
                type: "character varying(16)",
                maxLength: 16,
                nullable: false,
                oldClrType: typeof(int),
                oldType: "integer");

            migrationBuilder.Sql(
                "UPDATE crypter.\"UserConsent\" SET \"ConsentType\" = 'TermsOfService' WHERE \"ConsentType\" = '0';" +
                "UPDATE crypter.\"UserConsent\" SET \"ConsentType\" = 'PrivacyPolicy' WHERE \"ConsentType\" = '1';" +
                "UPDATE crypter.\"UserConsent\" SET \"ConsentType\" = 'RecoveryKeyRisks' WHERE \"ConsentType\" = '2';");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                "UPDATE crypter.\"UserToken\" SET \"Type\" = '0' WHERE \"Type\" = 'Authentication';" +
                "UPDATE crypter.\"UserToken\" SET \"Type\" = '1' WHERE \"Type\" = 'Session';" + 
                "UPDATE crypter.\"UserToken\" SET \"Type\" = '2' WHERE \"Type\" = 'Device';");

            migrationBuilder.Sql(
                "ALTER TABLE crypter.\"UserToken\" ALTER COLUMN \"Type\" TYPE INT USING \"Type\"::integer;");
            
            migrationBuilder.Sql(
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = '0' WHERE \"Visibility\" = 'None';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = '1' WHERE \"Visibility\" = 'Contacts';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = '2' WHERE \"Visibility\" = 'Authenticated';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"Visibility\" = '3' WHERE \"Visibility\" = 'Everyone';");
            
            migrationBuilder.Sql(
                "ALTER TABLE crypter.\"UserPrivacySetting\" ALTER COLUMN \"Visibility\" TYPE INT USING \"Visibility\"::integer;");

            migrationBuilder.Sql(
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = '0' WHERE \"ReceiveMessages\" = 'None';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = '1' WHERE \"ReceiveMessages\" = 'ExchangedKeys';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = '2' WHERE \"ReceiveMessages\" = 'Contacts';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = '3' WHERE \"ReceiveMessages\" = 'Authenticated';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveMessages\" = '4' WHERE \"ReceiveMessages\" = 'Everyone';");
            
            migrationBuilder.Sql(
                "ALTER TABLE crypter.\"UserPrivacySetting\" ALTER COLUMN \"ReceiveMessages\" TYPE INT USING \"ReceiveMessages\"::integer;");

            migrationBuilder.Sql(
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = '0' WHERE \"ReceiveFiles\" = 'None';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = '1' WHERE \"ReceiveFiles\" = 'ExchangedKeys';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = '2' WHERE \"ReceiveFiles\" = 'Contacts';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = '3' WHERE \"ReceiveFiles\" = 'Authenticated';" +
                "UPDATE crypter.\"UserPrivacySetting\" SET \"ReceiveFiles\" = '4' WHERE \"ReceiveFiles\" = 'Everyone';");
            
            migrationBuilder.Sql(
                "ALTER TABLE crypter.\"UserPrivacySetting\" ALTER COLUMN \"ReceiveFiles\" TYPE INT USING \"ReceiveFiles\"::integer;");
            
            migrationBuilder.Sql(
                "UPDATE crypter.\"UserConsent\" SET \"ConsentType\" = '0' WHERE \"ConsentType\" = 'TermsOfService';" +
                "UPDATE crypter.\"UserConsent\" SET \"ConsentType\" = '1' WHERE \"ConsentType\" = 'PrivacyPolicy';" +
                "UPDATE crypter.\"UserConsent\" SET \"ConsentType\" = '2' WHERE \"ConsentType\" = 'RecoveryKeyRisks';");
            
            migrationBuilder.Sql(
                "ALTER TABLE crypter.\"UserConsent\" ALTER COLUMN \"ConsentType\" TYPE INT USING \"ConsentType\"::integer;");
        }
    }
}
