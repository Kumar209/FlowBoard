using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Project.Service.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddFullText : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // FULLTEXT for Tasks.Title search — allows CONTAINS(Title, @search) via SqlServer
            // Best-effort: if FullText service not available (MonsterASP.net may restrict), migration still applies but search falls back to LIKE
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'FlowBoardCatalog')
                    CREATE FULLTEXT CATALOG FlowBoardCatalog AS DEFAULT;
            ", suppressTransaction: true);
            migrationBuilder.Sql(@"
                IF NOT EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('[project].[Tasks]'))
                BEGIN
                    -- Ensure unique index exists for FullText key (PK already unique)
                    CREATE FULLTEXT INDEX ON [project].[Tasks](Title LANGUAGE 1033) KEY INDEX PK_Tasks ON FlowBoardCatalog WITH CHANGE_TRACKING AUTO;
                END
            ", suppressTransaction: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.fulltext_indexes WHERE object_id = OBJECT_ID('[project].[Tasks]')) DROP FULLTEXT INDEX ON [project].[Tasks];", suppressTransaction: true);
            migrationBuilder.Sql("IF EXISTS (SELECT * FROM sys.fulltext_catalogs WHERE name = 'FlowBoardCatalog') DROP FULLTEXT CATALOG FlowBoardCatalog;", suppressTransaction: true);
        }
    }
}
