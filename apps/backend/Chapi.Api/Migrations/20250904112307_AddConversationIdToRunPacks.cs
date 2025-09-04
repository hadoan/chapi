using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConversationIdToRunPacks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "ConversationId",
                table: "RunPacks",
                type: "uuid",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_RunPacks_ConversationId",
                table: "RunPacks",
                column: "ConversationId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RunPacks_ConversationId",
                table: "RunPacks");

            migrationBuilder.DropColumn(
                name: "ConversationId",
                table: "RunPacks");
        }
    }
}
