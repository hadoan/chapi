using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class NormalizeJsonColumnsAndFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "ConsumesJson",
                schema: "EndpointCatalog",
                table: "ApiEndpoints",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasRequestBody",
                schema: "EndpointCatalog",
                table: "ApiEndpoints",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "ProducesJson",
                schema: "EndpointCatalog",
                table: "ApiEndpoints",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "RequiresAuth",
                schema: "EndpointCatalog",
                table: "ApiEndpoints",
                type: "boolean",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConsumesJson",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.DropColumn(
                name: "HasRequestBody",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.DropColumn(
                name: "ProducesJson",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.DropColumn(
                name: "RequiresAuth",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");
        }
    }
}
