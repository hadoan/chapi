using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    public partial class NormalizeJsonColumnsAndFlags : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "RequiresAuth",
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
                name: "ConsumesJson",
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

            // Ensure jsonb + unwrap any stringified JSON
            migrationBuilder.Sql(@"
ALTER TABLE \"EndpointCatalog\".\"ApiEndpoints\"
  ALTER COLUMN \"Servers\"   TYPE jsonb USING
    CASE
      WHEN \"Servers\" IS NULL THEN NULL
      WHEN left(\"Servers\"::text,1) = '"' THEN trim(both '"' from \"Servers\"::text)::jsonb
      ELSE \"Servers\"::jsonb
    END,
  ALTER COLUMN \"Security\"  TYPE jsonb USING
    CASE
      WHEN \"Security\" IS NULL THEN NULL
      WHEN left(\"Security\"::text,1) = '"' THEN trim(both '"' from \"Security\"::text)::jsonb
      ELSE \"Security\"::jsonb
    END,
  ALTER COLUMN \"Parameters\" TYPE jsonb USING
    CASE
      WHEN \"Parameters\" IS NULL OR \"Parameters\"::text IN ('"null"','null') THEN NULL
      WHEN left(\"Parameters\"::text,1) = '"' THEN trim(both '"' from \"Parameters\"::text)::jsonb
      ELSE \"Parameters\"::jsonb
    END,
  ALTER COLUMN \"Request\"    TYPE jsonb USING
    CASE
      WHEN \"Request\" IS NULL OR \"Request\"::text IN ('"null"','null') THEN NULL
      WHEN left(\"Request\"::text,1) = '"' THEN trim(both '"' from \"Request\"::text)::jsonb
      ELSE \"Request\"::jsonb
    END,
  ALTER COLUMN \"Responses\"  TYPE jsonb USING
    CASE
      WHEN \"Responses\" IS NULL THEN NULL
      WHEN left(\"Responses\"::text,1) = '"' THEN trim(both '"' from \"Responses\"::text)::jsonb
      ELSE \"Responses\"::jsonb
    END;
");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RequiresAuth",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.DropColumn(
                name: "HasRequestBody",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.DropColumn(
                name: "ConsumesJson",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.DropColumn(
                name: "ProducesJson",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");
        }
    }
}
