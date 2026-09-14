using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Identity.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIsSuperAdmin : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsSuperAdmin",
                schema: "identity",
                table: "Users",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.Sql("UPDATE [identity].[Users] SET [IsSuperAdmin] = 1 WHERE LOWER([Email]) = 'superadmin@flowboard.local'");

            // SuperAdmin is global, not a tenant member. Remove dummy system org/workspace created in earlier seeder.
            // Delete in FK order: WorkspaceMembers -> OrganizationMembers -> Workspaces -> Organizations where Name = 'FlowBoard System'
            migrationBuilder.Sql(@"
                DECLARE @sysOrgId uniqueidentifier = (SELECT TOP 1 Id FROM [identity].[Organizations] WHERE Name = 'FlowBoard System');
                IF @sysOrgId IS NOT NULL
                BEGIN
                    DELETE wm FROM [identity].[WorkspaceMembers] wm INNER JOIN [identity].[Workspaces] w ON w.Id = wm.WorkspaceId WHERE w.OrganizationId = @sysOrgId;
                    DELETE FROM [identity].[OrganizationMembers] WHERE OrganizationId = @sysOrgId;
                    DELETE FROM [identity].[Workspaces] WHERE OrganizationId = @sysOrgId;
                    DELETE FROM [identity].[Organizations] WHERE Id = @sysOrgId;
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuperAdmin",
                schema: "identity",
                table: "Users");
        }
    }
}
