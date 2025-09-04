using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddRunsAndRunPacksDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RunPacks",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: true),
                    Mode = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "hybrid"),
                    FilesCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                    ZipUrl = table.Column<string>(type: "character varying(512)", maxLength: 512, nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "draft"),
                    GeneratorVersion = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CardHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    InputsHash = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    FinalizedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_RunPacks", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Runs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SuiteId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    FinishedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
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
                    table.PrimaryKey("PK_Runs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RunPackFiles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileId = table.Column<Guid>(type: "uuid", nullable: false),
                    Role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false, defaultValue: "GENERATED"),
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
                    table.PrimaryKey("PK_RunPackFiles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunPackFiles_Files_FileId",
                        column: x => x.FileId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_RunPackFiles_RunPacks_RunPackId",
                        column: x => x.RunPackId,
                        principalTable: "RunPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunPackInputs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunPackId = table.Column<Guid>(type: "uuid", nullable: false),
                    FileRolesJson = table.Column<string>(type: "text", nullable: false),
                    RoleContextsJson = table.Column<string>(type: "text", nullable: false),
                    EndpointsContext = table.Column<string>(type: "text", nullable: false),
                    AllowedOps = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    Environment = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ProjectId = table.Column<Guid>(type: "uuid", nullable: true),
                    SuiteId = table.Column<Guid>(type: "uuid", nullable: true),
                    AiModel = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Temperature = table.Column<double>(type: "double precision", nullable: true),
                    MaxTokens = table.Column<int>(type: "integer", nullable: true),
                    ContextSize = table.Column<int>(type: "integer", nullable: true),
                    StopSequences = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
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
                    table.PrimaryKey("PK_RunPackInputs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunPackInputs_RunPacks_RunPackId",
                        column: x => x.RunPackId,
                        principalTable: "RunPacks",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "RunSteps",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Log = table.Column<string>(type: "text", nullable: true),
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
                    table.PrimaryKey("PK_RunSteps", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RunSteps_Runs_RunId",
                        column: x => x.RunId,
                        principalTable: "Runs",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunPackFiles_FileId",
                table: "RunPackFiles",
                column: "FileId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPackFiles_Role",
                table: "RunPackFiles",
                column: "Role");

            migrationBuilder.CreateIndex(
                name: "IX_RunPackFiles_RunPackId",
                table: "RunPackFiles",
                column: "RunPackId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPackFiles_RunPackId_FileId",
                table: "RunPackFiles",
                columns: new[] { "RunPackId", "FileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunPackInputs_Environment",
                table: "RunPackInputs",
                column: "Environment");

            migrationBuilder.CreateIndex(
                name: "IX_RunPackInputs_ProjectId",
                table: "RunPackInputs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPackInputs_RunPackId",
                table: "RunPackInputs",
                column: "RunPackId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunPackInputs_SuiteId",
                table: "RunPackInputs",
                column: "SuiteId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPacks_CreatedAt",
                table: "RunPacks",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_RunPacks_ProjectId",
                table: "RunPacks",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPacks_RunId",
                table: "RunPacks",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_RunPacks_Status",
                table: "RunPacks",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_RunSteps_RunId",
                table: "RunSteps",
                column: "RunId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunPackFiles");

            migrationBuilder.DropTable(
                name: "RunPackInputs");

            migrationBuilder.DropTable(
                name: "RunSteps");

            migrationBuilder.DropTable(
                name: "RunPacks");

            migrationBuilder.DropTable(
                name: "Runs");
        }
    }
}
