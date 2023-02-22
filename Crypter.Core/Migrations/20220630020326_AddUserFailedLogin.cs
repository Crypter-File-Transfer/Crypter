using Microsoft.EntityFrameworkCore.Migrations;
using System;

#nullable disable

namespace Crypter.Core.Migrations
{
   public partial class AddUserFailedLogin : Migration
   {
      protected override void Up(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.CreateTable(
             name: "UserFailedLogin",
             columns: table => new
             {
                Id = table.Column<Guid>(type: "uuid", nullable: false),
                Owner = table.Column<Guid>(type: "uuid", nullable: false),
                Date = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
             },
             constraints: table =>
             {
                table.PrimaryKey("PK_UserFailedLogin", x => x.Id);
                table.ForeignKey(
                       name: "FK_UserFailedLogin_User_Owner",
                       column: x => x.Owner,
                       principalTable: "User",
                       principalColumn: "Id",
                       onDelete: ReferentialAction.Cascade);
             });

         migrationBuilder.CreateIndex(
             name: "IX_UserFailedLogin_Owner",
             table: "UserFailedLogin",
             column: "Owner");
      }

      protected override void Down(MigrationBuilder migrationBuilder)
      {
         migrationBuilder.DropTable(
             name: "UserFailedLogin");
      }
   }
}
