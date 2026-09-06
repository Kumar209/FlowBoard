using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardsAndSprints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoardId",
                schema: "project",
                table: "BoardLists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "BoardId1",
                schema: "project",
                table: "BoardLists",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Boards",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Boards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Boards_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Sprints",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    BoardId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Sprints", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Sprints_Boards_BoardId",
                        column: x => x.BoardId,
                        principalSchema: "project",
                        principalTable: "Boards",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Sprints_Projects_ProjectId",
                        column: x => x.ProjectId,
                        principalSchema: "project",
                        principalTable: "Projects",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoardLists_BoardId",
                schema: "project",
                table: "BoardLists",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardLists_BoardId1",
                schema: "project",
                table: "BoardLists",
                column: "BoardId1");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ProjectId",
                schema: "project",
                table: "Boards",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Boards_ProjectId_Position",
                schema: "project",
                table: "Boards",
                columns: new[] { "ProjectId", "Position" });

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_BoardId",
                schema: "project",
                table: "Sprints",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_BoardId_StartDate",
                schema: "project",
                table: "Sprints",
                columns: new[] { "BoardId", "StartDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Sprints_ProjectId",
                schema: "project",
                table: "Sprints",
                column: "ProjectId");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardLists_Boards_BoardId",
                schema: "project",
                table: "BoardLists",
                column: "BoardId",
                principalSchema: "project",
                principalTable: "Boards",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_BoardLists_Boards_BoardId1",
                schema: "project",
                table: "BoardLists",
                column: "BoardId1",
                principalSchema: "project",
                principalTable: "Boards",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardLists_Boards_BoardId",
                schema: "project",
                table: "BoardLists");

            migrationBuilder.DropForeignKey(
                name: "FK_BoardLists_Boards_BoardId1",
                schema: "project",
                table: "BoardLists");

            migrationBuilder.DropTable(
                name: "Sprints",
                schema: "project");

            migrationBuilder.DropTable(
                name: "Boards",
                schema: "project");

            migrationBuilder.DropIndex(
                name: "IX_BoardLists_BoardId",
                schema: "project",
                table: "BoardLists");

            migrationBuilder.DropIndex(
                name: "IX_BoardLists_BoardId1",
                schema: "project",
                table: "BoardLists");

            migrationBuilder.DropColumn(
                name: "BoardId",
                schema: "project",
                table: "BoardLists");

            migrationBuilder.DropColumn(
                name: "BoardId1",
                schema: "project",
                table: "BoardLists");
        }
    }
}
