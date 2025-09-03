using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Chapi.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddConcurencyStamp : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "SubscriptionUsages");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "Environment",
                table: "SecretVaultRefs");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "Project",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "Project",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "LlmLogs");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "Environment",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "Environment",
                table: "EnvironmentHeaders");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "CredentialField");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "ApiSpecs",
                table: "ApiSpecs");

            migrationBuilder.DropColumn(
                name: "EntityVersion",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "UserSubscriptions",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Users",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "SubscriptionUsages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "SubscriptionPlans",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Environment",
                table: "SecretVaultRefs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Project",
                table: "ProjectTasks",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Project",
                table: "Projects",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Messages",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "LlmLogs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Integrations",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "IntegrationCredentials",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Files",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Environment",
                table: "Environments",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "Environment",
                table: "EnvironmentHeaders",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "CredentialField",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Conversations",
                type: "character varying(40)",
                maxLength: 40,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "Contacts",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "ApiSpecs",
                table: "ApiSpecs",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                schema: "EndpointCatalog",
                table: "ApiEndpoints",
                type: "text",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "UserSubscriptions");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "SubscriptionUsages");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "SubscriptionPlans");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Environment",
                table: "SecretVaultRefs");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Project",
                table: "ProjectTasks");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Project",
                table: "Projects");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Messages");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "LlmLogs");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Integrations");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "IntegrationCredentials");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Files");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Environment",
                table: "Environments");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "Environment",
                table: "EnvironmentHeaders");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "CredentialField");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Conversations");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "Contacts");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "ApiSpecs",
                table: "ApiSpecs");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                schema: "EndpointCatalog",
                table: "ApiEndpoints");

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "UserSubscriptions",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "Users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "SubscriptionUsages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "SubscriptionPlans",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "Environment",
                table: "SecretVaultRefs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "Project",
                table: "ProjectTasks",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "Project",
                table: "Projects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "Messages",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "LlmLogs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "Integrations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "IntegrationCredentials",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "Files",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "Environment",
                table: "Environments",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "Environment",
                table: "EnvironmentHeaders",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "CredentialField",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "Conversations",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                table: "Contacts",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "ApiSpecs",
                table: "ApiSpecs",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "EntityVersion",
                schema: "EndpointCatalog",
                table: "ApiEndpoints",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
