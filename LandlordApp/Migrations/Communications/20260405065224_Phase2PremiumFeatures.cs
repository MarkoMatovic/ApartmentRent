using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Communications
{
    /// <inheritdoc />
    public partial class Phase2PremiumFeatures : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Idempotent: column may already exist from a partial previous run
            migrationBuilder.Sql(@"
                IF NOT EXISTS (
                    SELECT 1 FROM sys.columns
                    WHERE object_id = OBJECT_ID('[Communication].[Messages]')
                    AND name = 'IsSuperLike'
                )
                BEGIN
                    ALTER TABLE [Communication].[Messages]
                    ADD [IsSuperLike] bit NOT NULL DEFAULT 0
                END
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsSuperLike",
                schema: "Communication",
                table: "Messages");
        }
    }
}
