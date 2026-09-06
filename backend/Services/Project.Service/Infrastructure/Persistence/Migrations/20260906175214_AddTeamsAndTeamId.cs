using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamsAndTeamId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TeamId",
                schema: "project",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "BoardId",
                schema: "project",
                table: "Sprints",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.CreateTable(
                name: "Teams",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Teams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Teams_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TeamMembers",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TeamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    JoinedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TeamMembers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TeamMembers_Teams_TeamId",
                        column: x => x.TeamId,
                        principalSchema: "project",
                        principalTable: "Teams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_TeamId",
                schema: "project",
                table: "Tasks",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId",
                schema: "project",
                table: "TeamMembers",
                column: "TeamId");

            migrationBuilder.CreateIndex(
                name: "IX_TeamMembers_TeamId_UserId",
                schema: "project",
                table: "TeamMembers",
                columns: new[] { "TeamId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ProjectId",
                schema: "project",
                table: "Teams",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Teams_ProjectId_Name",
                schema: "project",
                table: "Teams",
                columns: new[] { "ProjectId", "Name" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TeamMembers",
                schema: "project");

            migrationBuilder.DropTable(
                name: "Teams",
                schema: "project");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_TeamId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "TeamId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "BoardId",
                schema: "project",
                table: "Sprints",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);
        }
    }
}
