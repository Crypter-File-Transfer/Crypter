using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Crypter.DataAccess.Migrations
{
   public partial class InitialCreate : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable(
             name: "FileTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Sender = table.Column<Guid>(type: "uuid", nullable: false),
                Recipient = table.Column<Guid>(type: "uuid", nullable: false),
                FileName = table.Column<string>(type: "text", nullable: true),
                ContentType = table.Column<string>(type: "text", nullable: true),
                Size = table.Column<int>(type: "integer", nullable: false),
                ClientIV = table.Column<string>(type: "text", nullable: true),
                Signature = table.Column<string>(type: "text", nullable: true),
                X25519PublicKey = table.Column<string>(type: "text", nullable: true),
                Ed25519PublicKey = table.Column<string>(type: "text", nullable: true),
                ServerIV = table.Column<byte[]>(type: "bytea", nullable: true),
                ServerDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_FileTransfer", x => x.Id);
             });

         migrationBuilder.CreateTable(
             name: "MessageTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Sender = table.Column<Guid>(type: "uuid", nullable: false),
                Recipient = table.Column<Guid>(type: "uuid", nullable: false),
                Subject = table.Column<string>(type: "text", nullable: true),
                Size = table.Column<int>(type: "integer", nullable: false),
                ClientIV = table.Column<string>(type: "text", nullable: true),
                Signature = table.Column<string>(type: "text", nullable: true),
                X25519PublicKey = table.Column<string>(type: "text", nullable: true),
                Ed25519PublicKey = table.Column<string>(type: "text", nullable: true),
                ServerIV = table.Column<byte[]>(type: "bytea", nullable: true),
                ServerDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_MessageTransfer", x => x.Id);
             });

         migrationBuilder.CreateTable(
             name: "Schema",
             columns: table => new
             {
                Version = table.Column<int>(type: "integer", nullable: false),
                Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
             });

         migrationBuilder.CreateTable(
             name: "User",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Username = table.Column<string>(type: "text", nullable: true),
                Email = table.Column<string>(type: "text", nullable: true),
                PasswordHash = table.Column<byte[]>(type: "bytea", nullable: true),
                PasswordSalt = table.Column<byte[]>(type: "bytea", nullable: true),
                EmailVerified = table.Column<bool>(type: "boolean", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_User", x => x.Id);
             });

         migrationBuilder.CreateTable(
             name: "UserContact",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                Contact = table.Column<Guid>(type: "uuid", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserContact", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserContact_User_Contact",
                       column: x => x.Contact,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                       name: "FK_UserContact_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserEd25519KeyPair",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                PrivateKey = table.Column<string>(type: "text", nullable: true),
                PublicKey = table.Column<string>(type: "text", nullable: true),
                ClientIV = table.Column<string>(type: "text", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserEd25519KeyPair", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserEd25519KeyPair_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserEmailVerification",
             columns: table => new
             {
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                Code = table.Column<Guid>(type: "uuid", nullable: false),
                VerificationKey = table.Column<byte[]>(type: "bytea", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserEmailVerification", x => x.Owner);
                table.ForeignKey(
                       name: "FK_UserEmailVerification_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserNotificationSetting",
             columns: table => new
             {
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                EnableTransferNotifications = table.Column<bool>(type: "boolean", nullable: false),
                EmailNotifications = table.Column<bool>(type: "boolean", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserNotificationSetting", x => x.Owner);
                table.ForeignKey(
                       name: "FK_UserNotificationSetting_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserPrivacySetting",
             columns: table => new
             {
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                AllowKeyExchangeRequests = table.Column<bool>(type: "boolean", nullable: false),
                Visibility = table.Column<int>(type: "integer", nullable: false),
                ReceiveFiles = table.Column<int>(type: "integer", nullable: false),
                ReceiveMessages = table.Column<int>(type: "integer", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserPrivacySetting", x => x.Owner);
                table.ForeignKey(
                       name: "FK_UserPrivacySetting_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserProfile",
             columns: table => new
             {
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                Alias = table.Column<string>(type: "text", nullable: true),
                About = table.Column<string>(type: "text", nullable: true),
                Image = table.Column<string>(type: "text", nullable: true)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserProfile", x => x.Owner);
                table.ForeignKey(
                       name: "FK_UserProfile_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserToken",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                Description = table.Column<string>(type: "text", nullable: true),
                Type = table.Column<int>(type: "integer", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserToken", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserToken_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateTable(
             name: "UserX25519KeyPair",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                PrivateKey = table.Column<string>(type: "text", nullable: true),
                PublicKey = table.Column<string>(type: "text", nullable: true),
                ClientIV = table.Column<string>(type: "text", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserX25519KeyPair", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserX25519KeyPair_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateIndex(
             name: "user_email_unique",
             table: "User",
             column: "Email");

         migrationBuilder.CreateIndex(
             name: "user_username_unique",
             table: "User",
             column: "Username");

         migrationBuilder.CreateIndex(
             name: "IX_UserContact_Contact",
             table: "UserContact",
             column: "Contact");

         migrationBuilder.CreateIndex(
             name: "IX_UserContact_Owner",
             table: "UserContact",
             column: "Owner");

         migrationBuilder.CreateIndex(
             name: "Idx_UserEd25519KeyPair_Owner",
             table: "UserEd25519KeyPair",
             column: "Owner",
             unique: true);

         migrationBuilder.CreateIndex(
             name: "IX_UserToken_Owner",
             table: "UserToken",
             column: "Owner");

         migrationBuilder.CreateIndex(
             name: "Idx_UserX25519KeyPair_Owner",
             table: "UserX25519KeyPair",
             column: "Owner",
             unique: true);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable(
             name: "FileTransfer");

         migrationBuilder.DropTable(
             name: "MessageTransfer");

         migrationBuilder.DropTable(
             name: "Schema");

         migrationBuilder.DropTable(
             name: "UserContact");

         migrationBuilder.DropTable(
             name: "UserEd25519KeyPair");

         migrationBuilder.DropTable(
             name: "UserEmailVerification");

         migrationBuilder.DropTable(
             name: "UserNotificationSetting");

         migrationBuilder.DropTable(
             name: "UserPrivacySetting");

         migrationBuilder.DropTable(
             name: "UserProfile");

         migrationBuilder.DropTable(
             name: "UserToken");

         migrationBuilder.DropTable(
             name: "UserX25519KeyPair");

         migrationBuilder.DropTable(
             name: "User");
      }
   }
}
