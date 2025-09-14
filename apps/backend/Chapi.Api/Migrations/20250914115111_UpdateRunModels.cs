using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRunModels : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Log",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "SuiteId",
                table: "Runs");

            migrationBuilder.RenameColumn(
                name: "Name",
                table: "RunSteps",
                newName: "StepId");

            migrationBuilder.AddColumn<int>(
                name: "DurationMs",
                table: "RunSteps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "RunSteps",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "FinishedAt",
                table: "RunSteps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "RunSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartedAt",
                table: "RunSteps",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "RunSteps",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "StatusCode",
                table: "RunSteps",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Type",
                table: "RunSteps",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Actor",
                table: "Runs",
                type: "character varying(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Error",
                table: "Runs",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IrPath",
                table: "Runs",
                type: "character varying(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ProjectId",
                table: "Runs",
                type: "uuid",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SuiteName",
                table: "Runs",
                type: "character varying(200)",
                maxLength: 200,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Trigger",
                table: "Runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Version",
                table: "Runs",
                type: "character varying(50)",
                maxLength: 50,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "RunEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RunId = table.Column<Guid>(type: "uuid", nullable: false),
                    StepId = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Kind = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Payload = table.Column<string>(type: "text", nullable: false),
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
                    table.PrimaryKey("PK_RunEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RunSteps_RunId_Order",
                table: "RunSteps",
                columns: new[] { "RunId", "Order" });

            migrationBuilder.CreateIndex(
                name: "IX_RunSteps_RunId_StepId",
                table: "RunSteps",
                columns: new[] { "RunId", "StepId" });

            migrationBuilder.CreateIndex(
                name: "IX_RunSteps_Status",
                table: "RunSteps",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_CreatedAt",
                table: "Runs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_ProjectId",
                table: "Runs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_ProjectId_Status",
                table: "Runs",
                columns: new[] { "ProjectId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Runs_Status",
                table: "Runs",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Runs_SuiteName_Version",
                table: "Runs",
                columns: new[] { "SuiteName", "Version" });

            migrationBuilder.CreateIndex(
                name: "IX_RunEvents_Kind",
                table: "RunEvents",
                column: "Kind");

            migrationBuilder.CreateIndex(
                name: "IX_RunEvents_RunId",
                table: "RunEvents",
                column: "RunId");

            migrationBuilder.CreateIndex(
                name: "IX_RunEvents_RunId_StepId",
                table: "RunEvents",
                columns: new[] { "RunId", "StepId" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RunEvents");

            migrationBuilder.DropIndex(
                name: "IX_RunSteps_RunId_Order",
                table: "RunSteps");

            migrationBuilder.DropIndex(
                name: "IX_RunSteps_RunId_StepId",
                table: "RunSteps");

            migrationBuilder.DropIndex(
                name: "IX_RunSteps_Status",
                table: "RunSteps");

            migrationBuilder.DropIndex(
                name: "IX_Runs_CreatedAt",
                table: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_Runs_ProjectId",
                table: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_Runs_ProjectId_Status",
                table: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_Runs_Status",
                table: "Runs");

            migrationBuilder.DropIndex(
                name: "IX_Runs_SuiteName_Version",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "DurationMs",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "FinishedAt",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "StartedAt",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "StatusCode",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Type",
                table: "RunSteps");

            migrationBuilder.DropColumn(
                name: "Actor",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "Error",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "IrPath",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "ProjectId",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "SuiteName",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "Trigger",
                table: "Runs");

            migrationBuilder.DropColumn(
                name: "Version",
                table: "Runs");

            migrationBuilder.RenameColumn(
                name: "StepId",
                table: "RunSteps",
                newName: "Name");

            migrationBuilder.AddColumn<string>(
                name: "Log",
                table: "RunSteps",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SuiteId",
                table: "Runs",
                type: "uuid",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));
        }
    }
}
