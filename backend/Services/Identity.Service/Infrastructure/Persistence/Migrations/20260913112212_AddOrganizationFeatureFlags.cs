using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddOrganizationFeatureFlags : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "OrganizationFeatureFlags",
                schema: "identity",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    OrganizationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FlagKey = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OrganizationFeatureFlags", x => x.Id);
                    table.ForeignKey(
                        name: "FK_OrganizationFeatureFlags_Organizations_OrganizationId",
                        column: x => x.OrganizationId,
                        principalSchema: "identity",
                        principalTable: "Organizations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationFeatureFlags_OrganizationId",
                schema: "identity",
                table: "OrganizationFeatureFlags",
                column: "OrganizationId");

            migrationBuilder.CreateIndex(
                name: "IX_OrganizationFeatureFlags_OrganizationId_FlagKey",
                schema: "identity",
                table: "OrganizationFeatureFlags",
                columns: new[] { "OrganizationId", "FlagKey" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OrganizationFeatureFlags",
                schema: "identity");
        }
    }
}
