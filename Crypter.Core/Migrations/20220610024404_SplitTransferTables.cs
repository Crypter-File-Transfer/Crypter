using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace Crypter.Core.Migrations
{
   public partial class SplitTransferTables : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable(
             name: "FileTransfer");

         migrationBuilder.DropTable(
             name: "MessageTransfer");

         migrationBuilder.RenameColumn(
             name: "Email",
             table: "User",
             newName: "EmailAddress");

         migrationBuilder.RenameIndex(
             name: "IX_User_Email",
             table: "User",
             newName: "IX_User_EmailAddress");

         migrationBuilder.CreateTable(
             name: "AnonymousFileTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Size = table.Column<int>(type: "integer", nullable: false),
                DigitalSignature = table.Column<string>(type: "text", nullable: false),
                DigitalSignaturePublicKey = table.Column<string>(type: "text", nullable: false),
                DiffieHellmanPublicKey = table.Column<string>(type: "text", nullable: false),
                RecipientProof = table.Column<string>(type: "text", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FileName = table.Column<string>(type: "text", nullable: false),
                ContentType = table.Column<string>(type: "text", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_AnonymousFileTransfer", x => x.Id);
             });

         migrationBuilder.CreateTable(
             name: "AnonymousMessageTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Size = table.Column<int>(type: "integer", nullable: false),
                DigitalSignature = table.Column<string>(type: "text", nullable: false),
                DigitalSignaturePublicKey = table.Column<string>(type: "text", nullable: false),
                DiffieHellmanPublicKey = table.Column<string>(type: "text", nullable: false),
                RecipientProof = table.Column<string>(type: "text", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Subject = table.Column<string>(type: "text", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_AnonymousMessageTransfer", x => x.Id);
             });

         migrationBuilder.CreateTable(
             name: "UserFileTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Size = table.Column<int>(type: "integer", nullable: false),
                DigitalSignature = table.Column<string>(type: "text", nullable: false),
                DigitalSignaturePublicKey = table.Column<string>(type: "text", nullable: false),
                DiffieHellmanPublicKey = table.Column<string>(type: "text", nullable: false),
                RecipientProof = table.Column<string>(type: "text", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Sender = table.Column<Guid>(type: "uuid", nullable: true),
                Recipient = table.Column<Guid>(type: "uuid", nullable: true),
                FileName = table.Column<string>(type: "text", nullable: false),
                ContentType = table.Column<string>(type: "text", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserFileTransfer", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserFileTransfer_User_Recipient",
                       column: x => x.Recipient,
                       principalTable: "User",
                       principalColumn: "Id");
                table.ForeignKey(
                       name: "FK_UserFileTransfer_User_Sender",
                       column: x => x.Sender,
                       principalTable: "User",
                       principalColumn: "Id");
             });

         migrationBuilder.CreateTable(
             name: "UserMessageTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Size = table.Column<int>(type: "integer", nullable: false),
                DigitalSignature = table.Column<string>(type: "text", nullable: false),
                DigitalSignaturePublicKey = table.Column<string>(type: "text", nullable: false),
                DiffieHellmanPublicKey = table.Column<string>(type: "text", nullable: false),
                RecipientProof = table.Column<string>(type: "text", nullable: false),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Sender = table.Column<Guid>(type: "uuid", nullable: true),
                Recipient = table.Column<Guid>(type: "uuid", nullable: true),
                Subject = table.Column<string>(type: "text", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserMessageTransfer", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserMessageTransfer_User_Recipient",
                       column: x => x.Recipient,
                       principalTable: "User",
                       principalColumn: "Id");
                table.ForeignKey(
                       name: "FK_UserMessageTransfer_User_Sender",
                       column: x => x.Sender,
                       principalTable: "User",
                       principalColumn: "Id");
             });

         migrationBuilder.CreateIndex(
             name: "IX_UserFileTransfer_Recipient",
             table: "UserFileTransfer",
             column: "Recipient");

         migrationBuilder.CreateIndex(
             name: "IX_UserFileTransfer_Sender",
             table: "UserFileTransfer",
             column: "Sender");

         migrationBuilder.CreateIndex(
             name: "IX_UserMessageTransfer_Recipient",
             table: "UserMessageTransfer",
             column: "Recipient");

         migrationBuilder.CreateIndex(
             name: "IX_UserMessageTransfer_Sender",
             table: "UserMessageTransfer",
             column: "Sender");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable(
             name: "AnonymousFileTransfer");

         migrationBuilder.DropTable(
             name: "AnonymousMessageTransfer");

         migrationBuilder.DropTable(
             name: "UserFileTransfer");

         migrationBuilder.DropTable(
             name: "UserMessageTransfer");

         migrationBuilder.RenameColumn(
             name: "EmailAddress",
             table: "User",
             newName: "Email");

         migrationBuilder.RenameIndex(
             name: "IX_User_EmailAddress",
             table: "User",
             newName: "IX_User_Email");

         migrationBuilder.CreateTable(
             name: "FileTransfer",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                ClientIV = table.Column<string>(type: "text", nullable: true),
                ContentType = table.Column<string>(type: "text", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Ed25519PublicKey = table.Column<string>(type: "text", nullable: true),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                FileName = table.Column<string>(type: "text", nullable: true),
                Recipient = table.Column<Guid>(type: "uuid", nullable: false),
                Sender = table.Column<Guid>(type: "uuid", nullable: false),
                ServerDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                ServerIV = table.Column<byte[]>(type: "bytea", nullable: true),
                Signature = table.Column<string>(type: "text", nullable: true),
                Size = table.Column<int>(type: "integer", nullable: false),
                X25519PublicKey = table.Column<string>(type: "text", nullable: true)
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
                ClientIV = table.Column<string>(type: "text", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Ed25519PublicKey = table.Column<string>(type: "text", nullable: true),
                Expiration = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Recipient = table.Column<Guid>(type: "uuid", nullable: false),
                Sender = table.Column<Guid>(type: "uuid", nullable: false),
                ServerDigest = table.Column<byte[]>(type: "bytea", nullable: true),
                ServerIV = table.Column<byte[]>(type: "bytea", nullable: true),
                Signature = table.Column<string>(type: "text", nullable: true),
                Size = table.Column<int>(type: "integer", nullable: false),
                Subject = table.Column<string>(type: "text", nullable: true),
                X25519PublicKey = table.Column<string>(type: "text", nullable: true)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_MessageTransfer", x => x.Id);
             });
      }
   }
}
