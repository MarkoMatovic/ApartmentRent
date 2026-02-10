using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Roommates
{
    /// <inheritdoc />
    public partial class AddOptimizedIndexes_Phase1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Roommates_AvailableFrom",
                schema: "Roommates",
                table: "Roommates",
                column: "AvailableFrom");

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_BudgetMax",
                schema: "Roommates",
                table: "Roommates",
                column: "BudgetMax");

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_BudgetMin",
                schema: "Roommates",
                table: "Roommates",
                column: "BudgetMin");

            migrationBuilder.CreateIndex(
                name: "IX_Roommates_CreatedDate",
                schema: "Roommates",
                table: "Roommates",
                column: "CreatedDate");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Roommates_AvailableFrom",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_BudgetMax",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_BudgetMin",
                schema: "Roommates",
                table: "Roommates");

            migrationBuilder.DropIndex(
                name: "IX_Roommates_CreatedDate",
                schema: "Roommates",
                table: "Roommates");
        }
    }
}
