using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAiLogs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AiUsageLogs",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrgId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    WorkspaceId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TaskId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Operation = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Provider = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Model = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    InputTokens = table.Column<int>(type: "int", nullable: false),
                    OutputTokens = table.Column<int>(type: "int", nullable: false),
                    TotalTokens = table.Column<int>(type: "int", nullable: false),
                    Cost = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    FailureReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FallbackUsed = table.Column<bool>(type: "bit", nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: false),
                    PromptHash = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    PromptPreview = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ResponsePreview = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AiUsageLogs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_CreatedAt",
                schema: "project",
                table: "AiUsageLogs",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_OrgId",
                schema: "project",
                table: "AiUsageLogs",
                column: "OrgId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_ProjectId",
                schema: "project",
                table: "AiUsageLogs",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_Provider_Model",
                schema: "project",
                table: "AiUsageLogs",
                columns: new[] { "Provider", "Model" });

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_TaskId",
                schema: "project",
                table: "AiUsageLogs",
                column: "TaskId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_UserId",
                schema: "project",
                table: "AiUsageLogs",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AiUsageLogs_WorkspaceId",
                schema: "project",
                table: "AiUsageLogs",
                column: "WorkspaceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AiUsageLogs",
                schema: "project");
        }
    }
}
