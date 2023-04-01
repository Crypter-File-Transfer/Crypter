using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.Core.Migrations
{
   public partial class AddCompressionType : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AddColumn<int>(
             name: "CompressionType",
             table: "UserMessageTransfer",
             type: "integer",
             nullable: false,
             defaultValue: 0);

         migrationBuilder.AddColumn<int>(
             name: "CompressionType",
             table: "UserFileTransfer",
             type: "integer",
             nullable: false,
             defaultValue: 0);

         migrationBuilder.AddColumn<int>(
             name: "CompressionType",
             table: "AnonymousMessageTransfer",
             type: "integer",
             nullable: false,
             defaultValue: 0);

         migrationBuilder.AddColumn<int>(
             name: "CompressionType",
             table: "AnonymousFileTransfer",
             type: "integer",
             nullable: false,
             defaultValue: 0);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropColumn(
             name: "CompressionType",
             table: "UserMessageTransfer");

         migrationBuilder.DropColumn(
             name: "CompressionType",
             table: "UserFileTransfer");

         migrationBuilder.DropColumn(
             name: "CompressionType",
             table: "AnonymousMessageTransfer");

         migrationBuilder.DropColumn(
             name: "CompressionType",
             table: "AnonymousFileTransfer");
      }
   }
}
