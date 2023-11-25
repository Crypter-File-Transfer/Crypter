using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
   /// <inheritdoc />
   public partial class RemovedTransferSigning : Migration
   {
      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable(
             name: "UserEd25519KeyPair");

         migrationBuilder.DropColumn(
             name: "DigitalSignature",
             table: "UserMessageTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignaturePublicKey",
             table: "UserMessageTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignature",
             table: "UserFileTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignaturePublicKey",
             table: "UserFileTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignature",
             table: "AnonymousMessageTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignaturePublicKey",
             table: "AnonymousMessageTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignature",
             table: "AnonymousFileTransfer");

         migrationBuilder.DropColumn(
             name: "DigitalSignaturePublicKey",
             table: "AnonymousFileTransfer");
      }

      /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<string>(
             name: "DigitalSignature",
             table: "UserMessageTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignaturePublicKey",
             table: "UserMessageTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignature",
             table: "UserFileTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignaturePublicKey",
             table: "UserFileTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignature",
             table: "AnonymousMessageTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignaturePublicKey",
             table: "AnonymousMessageTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignature",
             table: "AnonymousFileTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.AddColumn<string>(
             name: "DigitalSignaturePublicKey",
             table: "AnonymousFileTransfer",
             type: "text",
             nullable: false,
             defaultValue: "");

         migrationBuilder.CreateTable(
             name: "UserEd25519KeyPair",
             columns: table => new
             {
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                ClientIV = table.Column<string>(type: "text", nullable: true),
                Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                PrivateKey = table.Column<string>(type: "text", nullable: true),
                PublicKey = table.Column<string>(type: "text", nullable: true)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserEd25519KeyPair", x => x.Owner);
                table.ForeignKey(
                       name: "FK_UserEd25519KeyPair_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });
      }
   }
}
