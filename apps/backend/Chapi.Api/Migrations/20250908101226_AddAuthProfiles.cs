using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddAuthProfiles : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuthProfiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    ServiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    EnvironmentKey = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Type = table.Column<int>(type: "integer", nullable: false),
                    TokenUrl = table.Column<string>(type: "text", nullable: true),
                    AuthorizationUrl = table.Column<string>(type: "text", nullable: true),
                    Audience = table.Column<string>(type: "text", nullable: true),
                    ScopesCsv = table.Column<string>(type: "text", nullable: true),
                    InjectionMode = table.Column<int>(type: "integer", nullable: false),
                    InjectionName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    InjectionFormat = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false),
                    DetectSource = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    DetectConfidence = table.Column<double>(type: "double precision", nullable: false),
                    Enabled = table.Column<bool>(type: "boolean", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: true),
                    RowVersion = table.Column<byte[]>(type: "bytea", rowVersion: true, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuthProfileSecretRefs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AuthProfileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    SecretRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Notes = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    IsDeleted = table.Column<bool>(type: "boolean", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    DeletedBy = table.Column<Guid>(type: "uuid", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuthProfileSecretRefs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AuthProfileSecretRefs_AuthProfiles_AuthProfileId",
                        column: x => x.AuthProfileId,
                        principalTable: "AuthProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AuthProfiles_ProjectId_ServiceId_EnvironmentKey",
                table: "AuthProfiles",
                columns: new[] { "ProjectId", "ServiceId", "EnvironmentKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AuthProfiles_ServiceId_EnvironmentKey_Enabled",
                table: "AuthProfiles",
                columns: new[] { "ServiceId", "EnvironmentKey", "Enabled" });

            migrationBuilder.CreateIndex(
                name: "IX_AuthProfileSecretRefs_AuthProfileId_Key",
                table: "AuthProfileSecretRefs",
                columns: new[] { "AuthProfileId", "Key" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuthProfileSecretRefs");

            migrationBuilder.DropTable(
                name: "AuthProfiles");
        }
    }
}
