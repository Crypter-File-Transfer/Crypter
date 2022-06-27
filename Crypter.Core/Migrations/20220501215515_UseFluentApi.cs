using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Crypter.Core.Migrations
{
   public partial class UseFluentApi : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropPrimaryKey(
             name: "PK_UserX25519KeyPair",
             table: "UserX25519KeyPair");

         migrationBuilder.DropIndex(
             name: "Idx_UserX25519KeyPair_Owner",
             table: "UserX25519KeyPair");

         migrationBuilder.DropPrimaryKey(
             name: "PK_UserEd25519KeyPair",
             table: "UserEd25519KeyPair");

         migrationBuilder.DropIndex(
             name: "Idx_UserEd25519KeyPair_Owner",
             table: "UserEd25519KeyPair");

         migrationBuilder.DropPrimaryKey(
             name: "PK_UserContact",
             table: "UserContact");

         migrationBuilder.DropIndex(
             name: "IX_UserContact_Owner",
             table: "UserContact");

         migrationBuilder.DropColumn(
             name: "Id",
             table: "UserX25519KeyPair");

         migrationBuilder.DropColumn(
             name: "Id",
             table: "UserEd25519KeyPair");

         migrationBuilder.DropColumn(
             name: "Id",
             table: "UserContact");

         migrationBuilder.DropIndex(
            name: "user_email_unique",
            table: "User");

         migrationBuilder.DropIndex(
            name: "user_username_unique",
            table: "User");

         migrationBuilder.AlterDatabase()
             .Annotation("Npgsql:PostgresExtension:citext", ",,");

         migrationBuilder.AlterColumn<string>(
             name: "Username",
             table: "User",
             type: "citext",
             nullable: true,
             oldClrType: typeof(string),
             oldType: "text",
             oldNullable: true);

         migrationBuilder.AlterColumn<string>(
             name: "Email",
             table: "User",
             type: "citext",
             nullable: true,
             oldClrType: typeof(string),
             oldType: "text",
             oldNullable: true);

         migrationBuilder.AddPrimaryKey(
             name: "PK_UserX25519KeyPair",
             table: "UserX25519KeyPair",
             column: "Owner");

         migrationBuilder.AddPrimaryKey(
             name: "PK_UserEd25519KeyPair",
             table: "UserEd25519KeyPair",
             column: "Owner");

         migrationBuilder.AddPrimaryKey(
             name: "PK_UserContact",
             table: "UserContact",
             columns: new[] { "Owner", "Contact" });

         migrationBuilder.CreateIndex(
             name: "IX_User_Email",
             table: "User",
             column: "Email",
             unique: true);

         migrationBuilder.CreateIndex(
             name: "IX_User_Username",
             table: "User",
             column: "Username",
             unique: true);
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropPrimaryKey(
             name: "PK_UserX25519KeyPair",
             table: "UserX25519KeyPair");

         migrationBuilder.DropPrimaryKey(
             name: "PK_UserEd25519KeyPair",
             table: "UserEd25519KeyPair");

         migrationBuilder.DropPrimaryKey(
             name: "PK_UserContact",
             table: "UserContact");

         migrationBuilder.DropIndex(
             name: "IX_User_Email",
             table: "User");

         migrationBuilder.DropIndex(
             name: "IX_User_Username",
             table: "User");

         migrationBuilder.AlterDatabase()
             .OldAnnotation("Npgsql:PostgresExtension:citext", ",,");

         migrationBuilder.AddColumn<Guid>(
             name: "Id",
             table: "UserX25519KeyPair",
             type: "uuid",
             nullable: false,
             defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

         migrationBuilder.AddColumn<Guid>(
             name: "Id",
             table: "UserEd25519KeyPair",
             type: "uuid",
             nullable: false,
             defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

         migrationBuilder.AddColumn<Guid>(
             name: "Id",
             table: "UserContact",
             type: "uuid",
             nullable: false,
             defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

         migrationBuilder.AlterColumn<string>(
             name: "Username",
             table: "User",
             type: "text",
             nullable: true,
             oldClrType: typeof(string),
             oldType: "citext",
             oldNullable: true);

         migrationBuilder.AlterColumn<string>(
             name: "Email",
             table: "User",
             type: "text",
             nullable: true,
             oldClrType: typeof(string),
             oldType: "citext",
             oldNullable: true);

         migrationBuilder.AddPrimaryKey(
             name: "PK_UserX25519KeyPair",
             table: "UserX25519KeyPair",
             column: "Id");

         migrationBuilder.AddPrimaryKey(
             name: "PK_UserEd25519KeyPair",
             table: "UserEd25519KeyPair",
             column: "Id");

         migrationBuilder.AddPrimaryKey(
             name: "PK_UserContact",
             table: "UserContact",
             column: "Id");

         migrationBuilder.CreateIndex(
             name: "IX_UserX25519KeyPair_Owner",
             table: "UserX25519KeyPair",
             column: "Owner",
             unique: true);

         migrationBuilder.CreateIndex(
             name: "IX_UserEd25519KeyPair_Owner",
             table: "UserEd25519KeyPair",
             column: "Owner",
             unique: true);

         migrationBuilder.CreateIndex(
             name: "IX_UserContact_Owner",
             table: "UserContact",
             column: "Owner");
      }
   }
}
