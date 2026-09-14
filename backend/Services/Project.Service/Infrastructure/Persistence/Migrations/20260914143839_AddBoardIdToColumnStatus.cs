using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddBoardIdToColumnStatus : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "BoardId",
                schema: "project",
                table: "BoardColumnStatuses",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.Sql("UPDATE [project].[BoardColumnStatuses] SET BoardId = (SELECT BoardId FROM [project].[BoardLists] WHERE Id = ColumnId) WHERE BoardId = '00000000-0000-0000-0000-000000000000' AND EXISTS (SELECT 1 FROM [project].[BoardLists] WHERE Id = ColumnId AND BoardId IS NOT NULL)");
            migrationBuilder.Sql("UPDATE [project].[BoardColumnStatuses] SET BoardId = (SELECT TOP 1 Id FROM [project].[Boards] WHERE ProjectId = (SELECT ProjectId FROM [project].[BoardLists] WHERE Id = ColumnId)) WHERE BoardId = '00000000-0000-0000-0000-000000000000'");
            migrationBuilder.Sql(@"
                WITH cte AS (
                    SELECT ColumnId, StatusId, BoardId,
                           ROW_NUMBER() OVER (PARTITION BY BoardId, StatusId ORDER BY ColumnId) as rn
                    FROM [project].[BoardColumnStatuses] WHERE BoardId != '00000000-0000-0000-0000-000000000000'
                )
                DELETE FROM cte WHERE rn > 1
            ");

            migrationBuilder.CreateIndex(
                name: "IX_BoardColumnStatuses_BoardId",
                schema: "project",
                table: "BoardColumnStatuses",
                column: "BoardId");

            migrationBuilder.CreateIndex(
                name: "IX_BoardColumnStatuses_BoardId_StatusId",
                schema: "project",
                table: "BoardColumnStatuses",
                columns: new[] { "BoardId", "StatusId" },
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_BoardColumnStatuses_Boards_BoardId",
                schema: "project",
                table: "BoardColumnStatuses",
                column: "BoardId",
                principalSchema: "project",
                principalTable: "Boards",
                principalColumn: "Id",
                onDelete: ReferentialAction.NoAction);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_BoardColumnStatuses_Boards_BoardId",
                schema: "project",
                table: "BoardColumnStatuses");

            migrationBuilder.DropIndex(
                name: "IX_BoardColumnStatuses_BoardId",
                schema: "project",
                table: "BoardColumnStatuses");

            migrationBuilder.DropIndex(
                name: "IX_BoardColumnStatuses_BoardId_StatusId",
                schema: "project",
                table: "BoardColumnStatuses");

            migrationBuilder.DropColumn(
                name: "BoardId",
                schema: "project",
                table: "BoardColumnStatuses");
        }
    }
}
