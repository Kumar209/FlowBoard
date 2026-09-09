using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddStatusesAndColumnMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_BoardLists_ListId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ListId",
                schema: "project",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<Guid>(
                name: "StatusId",
                schema: "project",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Statuses",
                schema: "project",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProjectId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Statuses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "BoardColumnStatuses",
                schema: "project",
                columns: table => new
                {
                    ColumnId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    StatusId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoardColumnStatuses", x => new { x.ColumnId, x.StatusId });
                    table.ForeignKey(
                        name: "FK_BoardColumnStatuses_BoardLists_ColumnId",
                        column: x => x.ColumnId,
                        principalSchema: "project",
                        principalTable: "BoardLists",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BoardColumnStatuses_Statuses_StatusId",
                        column: x => x.StatusId,
                        principalSchema: "project",
                        principalTable: "Statuses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_StatusId",
                schema: "project",
                table: "Tasks",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardColumnStatuses_ColumnId",
                schema: "project",
                table: "BoardColumnStatuses",
                column: "ColumnId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardColumnStatuses_StatusId",
                schema: "project",
                table: "BoardColumnStatuses",
                column: "StatusId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_ProjectId",
                schema: "project",
                table: "Statuses",
                column: "ProjectId");

            migrationBuilder.CreateIndex(
                name: "IX_Statuses_ProjectId_Name",
                schema: "project",
                table: "Statuses",
                columns: new[] { "ProjectId", "Name" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_BoardLists_ListId",
                schema: "project",
                table: "Tasks",
                column: "ListId",
                principalSchema: "project",
                principalTable: "BoardLists",
                principalColumn: "Id");

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_Statuses_StatusId",
                schema: "project",
                table: "Tasks",
                column: "StatusId",
                principalSchema: "project",
                principalTable: "Statuses",
                principalColumn: "Id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_BoardLists_ListId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropForeignKey(
                name: "FK_Tasks_Statuses_StatusId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropTable(
                name: "BoardColumnStatuses",
                schema: "project");

            migrationBuilder.DropTable(
                name: "Statuses",
                schema: "project");

            migrationBuilder.DropIndex(
                name: "IX_Tasks_StatusId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "StatusId",
                schema: "project",
                table: "Tasks");

            migrationBuilder.AlterColumn<Guid>(
                name: "ListId",
                schema: "project",
                table: "Tasks",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Tasks_BoardLists_ListId",
                schema: "project",
                table: "Tasks",
                column: "ListId",
                principalSchema: "project",
                principalTable: "BoardLists",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
