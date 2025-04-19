using Crypter.Common.Enums;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Crypter.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class AddTransferTierTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApplicationSetting",
                schema: "crypter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    FreeTransferQuota = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApplicationSetting", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TransferTier",
                schema: "crypter",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Name = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    MaximumUploadSize = table.Column<long>(type: "bigint", nullable: false),
                    UserQuota = table.Column<long>(type: "bigint", nullable: false),
                    DefaultForUserCategory = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TransferTier", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TransferTier_DefaultForUserCategory",
                schema: "crypter",
                table: "TransferTier",
                column: "DefaultForUserCategory",
                unique: true);
            
            migrationBuilder.InsertData(
                schema: "crypter",
                table: "TransferTier",
                columns: new[] { "Name", "Description", "MaximumUploadSize", "UserQuota", "DefaultForUserCategory" },
                values: new object[,]
                {
                    { 
                        "Anonymous Users", 
                        null, 
                        1000000L, // 1 MB in bytes
                        1000000000L, // 1 GB in bytes
                        (int)UserCategory.Anonymous
                    },
                    { 
                        "Authenticated Users", 
                        null, 
                        1000000L, // 1 MB in bytes
                        1000000000L, // 1 GB in bytes
                        (int)UserCategory.Authenticated
                    },
                    { 
                        "Verified Users", 
                        null, 
                        1000000L, // 1 MB in bytes
                        1000000000L, // 1 GB in bytes
                        (int)UserCategory.Verified
                    }
                });
            
            migrationBuilder.InsertData(
                schema: "crypter",
                table: "ApplicationSetting",
                columns: new[] { "FreeTransferQuota" },
                values: new object[] { 1000000000L }  // 1 GB in bytes
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSetting",
                schema: "crypter");

            migrationBuilder.DropTable(
                name: "TransferTier",
                schema: "crypter");
        }
    }
}
