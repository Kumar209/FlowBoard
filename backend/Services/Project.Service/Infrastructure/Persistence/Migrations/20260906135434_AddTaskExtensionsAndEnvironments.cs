using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTaskExtensionsAndEnvironments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Environment",
                schema: "project",
                table: "Tasks",
                type: "nvarchar(50)",
                maxLength: 50,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Epic",
                schema: "project",
                table: "Tasks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IssueType",
                schema: "project",
                table: "Tasks",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "Task");

            migrationBuilder.AddColumn<string>(
                name: "LinkedIssuesJson",
                schema: "project",
                table: "Tasks",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ParentIssueId",
                schema: "project",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "SprintId",
                schema: "project",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartDate",
                schema: "project",
                table: "Tasks",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "StoryPoints",
                schema: "project",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeEstimated",
                schema: "project",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeRemaining",
                schema: "project",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TimeSpent",
                schema: "project",
                table: "Tasks",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "WatchersJson",
                schema: "project",
                table: "Tasks",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Environments",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Environments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Environments_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_ParentIssueId",
                schema: "project",
                table: "Tasks",
                column: "ParentIssueId");

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_SprintId",
                schema: "project",
                table: "Tasks",
                column: "SprintId");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ProjectId",
                schema: "project",
                table: "Environments",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Environments_ProjectId_Name",
                schema: "project",
                table: "Environments",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Environments",
                schema: "project");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_ParentIssueId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_SprintId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Environment",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Epic",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "IssueType",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "LinkedIssuesJson",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "ParentIssueId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "SprintId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StartDate",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StoryPoints",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TimeEstimated",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TimeRemaining",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TimeSpent",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "WatchersJson",
                schema: "project",
                table: "Tasks");
        }
    }
}
