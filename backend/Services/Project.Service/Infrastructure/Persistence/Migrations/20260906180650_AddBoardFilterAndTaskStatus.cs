using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardFilterAndTaskStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                schema: "project",
                table: "Tasks",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "To Do");

            migrationBuilder.AddColumn<string>(
                name: "FilterJson",
                schema: "project",
                table: "Boards",
                type: "nvarchar(2000)",
                maxLength: 2000,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Tasks_Status",
                schema: "project",
                table: "Tasks",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Tasks_Status",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "Status",
                schema: "project",
                table: "Tasks");

            migrationBuilder.DropColumn(
                name: "FilterJson",
                schema: "project",
                table: "Boards");
        }
    }
}
