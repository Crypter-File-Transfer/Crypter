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
                name: "DataTier",
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
                    table.PrimaryKey("PK_DataTier", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DataTier_DefaultForUserCategory",
                schema: "crypter",
                table: "DataTier",
                column: "DefaultForUserCategory",
                unique: true);
            
            migrationBuilder.InsertData(
                schema: "crypter",
                table: "DataTier",
                columns: new[] { "Name", "Description", "MaximumUploadSize", "UserQuota", "DefaultForUserCategory" },
                values: new object[,]
                {
                    { 
                        "Anonymous Users", 
                        null, 
                        104857600L, // 100 MB in bytes
                        5368709120L, // 5 GB in bytes
                        (int)UserCategory.Anonymous
                    },
                    { 
                        "Authenticated Users", 
                        null, 
                        262144000L, // 250 MB in bytes
                        10737418240L, // 10 GB in bytes
                        (int)UserCategory.Authenticated
                    },
                    { 
                        "Verified Users", 
                        null, 
                        262144000L, // 250 MB in bytes
                        10737418240L, // 10 GB in bytes
                        (int)UserCategory.Verified
                    }
                });
            
            migrationBuilder.InsertData(
                schema: "crypter",
                table: "ApplicationSetting",
                columns: new[] { "FreeTransferQuota" },
                values: new object[] { 10737418240L }  // 10 GB in bytes
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApplicationSetting",
                schema: "crypter");

            migrationBuilder.DropTable(
                name: "DataTier",
                schema: "crypter");
        }
    }
}
