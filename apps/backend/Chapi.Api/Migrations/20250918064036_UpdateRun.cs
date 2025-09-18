using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class UpdateRun : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "EnvironmentId",
                table: "Runs",
                type: "uuid",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EnvironmentId",
                table: "Runs");
        }
    }
}
