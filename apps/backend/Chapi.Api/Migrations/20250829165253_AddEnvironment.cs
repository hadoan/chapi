using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddEnvironment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Environment");

            migrationBuilder.CreateTable(
                name: "Environments",
                schema: "Environment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    BaseUrl = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    TimeoutMs = table.Column<int>(type: "integer", nullable: false),
                    FollowRedirects = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EnvironmentHeaders",
                schema: "Environment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Value = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EnvironmentHeaders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EnvironmentHeaders_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalSchema: "Environment",
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "SecretVaultRefs",
                schema: "Environment",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Provider = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    KeyPath = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    LastRotatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EnvironmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    EntityVersion = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SecretVaultRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SecretVaultRefs_Environments_EnvironmentId",
                        column: x => x.EnvironmentId,
                        principalSchema: "Environment",
                        principalTable: "Environments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_EnvironmentHeaders_EnvironmentId",
                schema: "Environment",
                table: "EnvironmentHeaders",
                column: "EnvironmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SecretVaultRefs_EnvironmentId",
                schema: "Environment",
                table: "SecretVaultRefs",
                column: "EnvironmentId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EnvironmentHeaders",
                schema: "Environment");

            migrationBuilder.DropTable(
                name: "SecretVaultRefs",
                schema: "Environment");

            migrationBuilder.DropTable(
                name: "Environments",
                schema: "Environment");
        }
    }
}
