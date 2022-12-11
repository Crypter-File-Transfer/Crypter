using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace Crypter.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddMasterKeyRecoveryKeyAndConsents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Schema");

            migrationBuilder.DropTable(
                name: "UserX25519KeyPair");

            migrationBuilder.DropColumn(
                name: "CompressionType",
                table: "UserMessageTransfer");

            migrationBuilder.DropColumn(
                name: "DiffieHellmanPublicKey",
                table: "UserMessageTransfer");

            migrationBuilder.DropColumn(
                name: "RecipientProof",
                table: "UserMessageTransfer");

            migrationBuilder.DropColumn(
                name: "CompressionType",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "DiffieHellmanPublicKey",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "RecipientProof",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "CompressionType",
                table: "AnonymousMessageTransfer");

            migrationBuilder.DropColumn(
                name: "DiffieHellmanPublicKey",
                table: "AnonymousMessageTransfer");

            migrationBuilder.DropColumn(
                name: "RecipientProof",
                table: "AnonymousMessageTransfer");

            migrationBuilder.DropColumn(
                name: "CompressionType",
                table: "AnonymousFileTransfer");

            migrationBuilder.DropColumn(
                name: "DiffieHellmanPublicKey",
                table: "AnonymousFileTransfer");

            migrationBuilder.DropColumn(
                name: "RecipientProof",
                table: "AnonymousFileTransfer");

            migrationBuilder.AddColumn<byte[]>(
                name: "KeyExchangeNonce",
                table: "UserMessageTransfer",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Proof",
                table: "UserMessageTransfer",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKey",
                table: "UserMessageTransfer",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "KeyExchangeNonce",
                table: "UserFileTransfer",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Proof",
                table: "UserFileTransfer",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKey",
                table: "UserFileTransfer",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<short>(
                name: "ClientPasswordVersion",
                table: "User",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<short>(
                name: "ServerPasswordVersion",
                table: "User",
                type: "smallint",
                nullable: false,
                defaultValue: (short)0);

            migrationBuilder.AddColumn<byte[]>(
                name: "KeyExchangeNonce",
                table: "AnonymousMessageTransfer",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Proof",
                table: "AnonymousMessageTransfer",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKey",
                table: "AnonymousMessageTransfer",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "KeyExchangeNonce",
                table: "AnonymousFileTransfer",
                type: "bytea",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "Proof",
                table: "AnonymousFileTransfer",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "PublicKey",
                table: "AnonymousFileTransfer",
                type: "bytea",
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "UserConsent",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityAlwaysColumn),
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    ConsentType = table.Column<int>(type: "integer", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserConsent", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserConsent_User_Owner",
                        column: x => x.Owner,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserKeyPair",
                columns: table => new
                {
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    PrivateKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    PublicKey = table.Column<byte[]>(type: "bytea", nullable: true),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: true),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserKeyPair", x => x.Owner);
                    table.ForeignKey(
                        name: "FK_UserKeyPair_User_Owner",
                        column: x => x.Owner,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserMasterKey",
                columns: table => new
                {
                    Owner = table.Column<Guid>(type: "uuid", nullable: false),
                    EncryptedKey = table.Column<byte[]>(type: "bytea", nullable: false),
                    Nonce = table.Column<byte[]>(type: "bytea", nullable: false),
                    RecoveryProof = table.Column<byte[]>(type: "bytea", nullable: true),
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Created = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserMasterKey", x => x.Owner);
                    table.ForeignKey(
                        name: "FK_UserMasterKey_User_Owner",
                        column: x => x.Owner,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserConsent_Owner",
                table: "UserConsent",
                column: "Owner");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserConsent");

            migrationBuilder.DropTable(
                name: "UserKeyPair");

            migrationBuilder.DropTable(
                name: "UserMasterKey");

            migrationBuilder.DropColumn(
                name: "KeyExchangeNonce",
                table: "UserMessageTransfer");

            migrationBuilder.DropColumn(
                name: "Proof",
                table: "UserMessageTransfer");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "UserMessageTransfer");

            migrationBuilder.DropColumn(
                name: "KeyExchangeNonce",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "Proof",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "UserFileTransfer");

            migrationBuilder.DropColumn(
                name: "ClientPasswordVersion",
                table: "User");

            migrationBuilder.DropColumn(
                name: "ServerPasswordVersion",
                table: "User");

            migrationBuilder.DropColumn(
                name: "KeyExchangeNonce",
                table: "AnonymousMessageTransfer");

            migrationBuilder.DropColumn(
                name: "Proof",
                table: "AnonymousMessageTransfer");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "AnonymousMessageTransfer");

            migrationBuilder.DropColumn(
                name: "KeyExchangeNonce",
                table: "AnonymousFileTransfer");

            migrationBuilder.DropColumn(
                name: "Proof",
                table: "AnonymousFileTransfer");

            migrationBuilder.DropColumn(
                name: "PublicKey",
                table: "AnonymousFileTransfer");

            migrationBuilder.AddColumn<int>(
                name: "CompressionType",
                table: "UserMessageTransfer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DiffieHellmanPublicKey",
                table: "UserMessageTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientProof",
                table: "UserMessageTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompressionType",
                table: "UserFileTransfer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DiffieHellmanPublicKey",
                table: "UserFileTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientProof",
                table: "UserFileTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompressionType",
                table: "AnonymousMessageTransfer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DiffieHellmanPublicKey",
                table: "AnonymousMessageTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientProof",
                table: "AnonymousMessageTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "CompressionType",
                table: "AnonymousFileTransfer",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "DiffieHellmanPublicKey",
                table: "AnonymousFileTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "RecipientProof",
                table: "AnonymousFileTransfer",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "Schema",
                columns: table => new
                {
                    Updated = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "UserX25519KeyPair",
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
                    table.PrimaryKey("PK_UserX25519KeyPair", x => x.Owner);
                    table.ForeignKey(
                        name: "FK_UserX25519KeyPair_User_Owner",
                        column: x => x.Owner,
                        principalTable: "User",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });
        }
    }
}
