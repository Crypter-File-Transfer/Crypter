using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
   /// <inheritdoc />
   public partial class UseLongTransferSize : Migration
   {
      /// <inheritdoc />
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AlterColumn<long>(
             name: "Size",
             table: "UserMessageTransfer",
             type: "bigint",
             nullable: false,
             oldClrType: typeof(int),
             oldType: "integer");

         migrationBuilder.AlterColumn<long>(
             name: "Size",
             table: "UserFileTransfer",
             type: "bigint",
             nullable: false,
             oldClrType: typeof(int),
             oldType: "integer");

         migrationBuilder.AlterColumn<long>(
             name: "Size",
             table: "AnonymousMessageTransfer",
             type: "bigint",
             nullable: false,
             oldClrType: typeof(int),
             oldType: "integer");

         migrationBuilder.AlterColumn<long>(
             name: "Size",
             table: "AnonymousFileTransfer",
             type: "bigint",
             nullable: false,
             oldClrType: typeof(int),
             oldType: "integer");
      }

      /// <inheritdoc />
      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.AlterColumn<int>(
             name: "Size",
             table: "UserMessageTransfer",
             type: "integer",
             nullable: false,
             oldClrType: typeof(long),
             oldType: "bigint");

         migrationBuilder.AlterColumn<int>(
             name: "Size",
             table: "UserFileTransfer",
             type: "integer",
             nullable: false,
             oldClrType: typeof(long),
             oldType: "bigint");

         migrationBuilder.AlterColumn<int>(
             name: "Size",
             table: "AnonymousMessageTransfer",
             type: "integer",
             nullable: false,
             oldClrType: typeof(long),
             oldType: "bigint");

         migrationBuilder.AlterColumn<int>(
             name: "Size",
             table: "AnonymousFileTransfer",
             type: "integer",
             nullable: false,
             oldClrType: typeof(long),
             oldType: "bigint");
      }
   }
}
