using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptionPlans : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "SubscriptionPlanId",
                schema: "identity",
                table: "Organizations",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateTable(
                name: "SubscriptionPlans",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MaxUsers = table.Column<int>(type: "int", nullable: false),
                    MaxWorkspaces = table.Column<int>(type: "int", nullable: false),
                    MaxProjects = table.Column<int>(type: "int", nullable: false),
                    StorageGB = table.Column<int>(type: "int", nullable: false),
                    AiRequests = table.Column<int>(type: "int", nullable: false),
                    ApiLimit = table.Column<int>(type: "int", nullable: false),
                    FeaturesJson = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubscriptionPlans", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Organizations_SubscriptionPlanId",
                schema: "identity",
                table: "Organizations",
                column: "SubscriptionPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_SubscriptionPlans_Name",
                schema: "identity",
                table: "SubscriptionPlans",
                column: "Name",
                unique: true);

            // Seed 4 plans with deterministic GUIDs (enum-aligned)
            migrationBuilder.InsertData(
                schema: "identity",
                table: "SubscriptionPlans",
                columns: new[] { "Id", "Name", "Price", "MaxUsers", "MaxWorkspaces", "MaxProjects", "StorageGB", "AiRequests", "ApiLimit", "FeaturesJson", "CreatedAt", "UpdatedAt" },
                values: new object[,]
                {
                    { new Guid("a0000000-0000-0000-0000-000000000010"), "Free", 0m, 5, 2, 3, 5, 100, 1000, "[\"basic-board\",\"basic-tasks\"]", DateTime.UtcNow, DateTime.UtcNow },
                    { new Guid("a0000000-0000-0000-0000-000000000011"), "Pro", 29m, 25, 10, 50, 50, 5000, 10000, "[\"board\",\"sprints\",\"ai-draft\"]", DateTime.UtcNow, DateTime.UtcNow },
                    { new Guid("a0000000-0000-0000-0000-000000000012"), "Business", 79m, 100, 50, 200, 200, 20000, 50000, "[\"board\",\"sprints\",\"ai-full\",\"analytics\"]", DateTime.UtcNow, DateTime.UtcNow },
                    { new Guid("a0000000-0000-0000-0000-000000000013"), "Enterprise", 199m, 500, 200, 1000, 1000, 100000, 200000, "[\"all\"]", DateTime.UtcNow, DateTime.UtcNow }
                });

            // Backfill existing orgs (empty Guid) to Free
            migrationBuilder.Sql("UPDATE [identity].[Organizations] SET [SubscriptionPlanId] = 'a0000000-0000-0000-0000-000000000010' WHERE [SubscriptionPlanId] = '00000000-0000-0000-0000-000000000000'");

            migrationBuilder.AddForeignKey(
                name: "FK_Organizations_SubscriptionPlans_SubscriptionPlanId",
                schema: "identity",
                table: "Organizations",
                column: "SubscriptionPlanId",
                principalSchema: "identity",
                principalTable: "SubscriptionPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Organizations_SubscriptionPlans_SubscriptionPlanId",
                schema: "identity",
                table: "Organizations");

            migrationBuilder.DropTable(
                name: "SubscriptionPlans",
                schema: "identity");

            migrationBuilder.DropIndex(
                name: "IX_Organizations_SubscriptionPlanId",
                schema: "identity",
                table: "Organizations");

            migrationBuilder.DropColumn(
                name: "SubscriptionPlanId",
                schema: "identity",
                table: "Organizations");
        }
    }
}
