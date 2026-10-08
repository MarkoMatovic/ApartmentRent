using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Payments
{
    /// <inheritdoc />
    public partial class PaymentOrderTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // NOTE: the Monri-era tables payments.Subscriptions and payments.Transactions are no
            // longer part of the model, so EF scaffolded DropTable for them. They are deliberately
            // NOT dropped here: payments.Transactions still holds historical rows. They simply stay
            // in the database, unmanaged. Drop them by hand after exporting the data if wanted.

            migrationBuilder.AddColumn<int>(
                name: "ApartmentId",
                schema: "payments",
                table: "ProcessedMonriOrders",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PlanId",
                schema: "payments",
                table: "ProcessedMonriOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ReversalReference",
                schema: "payments",
                table: "ProcessedMonriOrders",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ReversedAt",
                schema: "payments",
                table: "ProcessedMonriOrders",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                schema: "payments",
                table: "ProcessedMonriOrders",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProcessedMonriOrders_UserId",
                schema: "payments",
                table: "ProcessedMonriOrders",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_ProcessedMonriOrders_UserId",
                schema: "payments",
                table: "ProcessedMonriOrders");

            migrationBuilder.DropColumn(
                name: "ApartmentId",
                schema: "payments",
                table: "ProcessedMonriOrders");

            migrationBuilder.DropColumn(
                name: "PlanId",
                schema: "payments",
                table: "ProcessedMonriOrders");

            migrationBuilder.DropColumn(
                name: "ReversalReference",
                schema: "payments",
                table: "ProcessedMonriOrders");

            migrationBuilder.DropColumn(
                name: "ReversedAt",
                schema: "payments",
                table: "ProcessedMonriOrders");

            migrationBuilder.DropColumn(
                name: "UserId",
                schema: "payments",
                table: "ProcessedMonriOrders");

            // Subscriptions / Transactions were never dropped by Up(), so there is nothing to recreate.
        }
    }
}
