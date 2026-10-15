using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Billing
{
    /// <inheritdoc />
    public partial class AddBilling : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Billing");

            migrationBuilder.CreateTable(
                name: "BillingProfiles",
                schema: "Billing",
                columns: table => new
                {
                    UserId = table.Column<int>(type: "int", nullable: false),
                    PayoutName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    PayoutAccount = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillingProfiles", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Tenancies",
                schema: "Billing",
                columns: table => new
                {
                    TenancyId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApartmentId = table.Column<int>(type: "int", nullable: false),
                    LandlordUserId = table.Column<int>(type: "int", nullable: false),
                    ApartmentTitle = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    MonthlyRent = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    RentDueDay = table.Column<int>(type: "int", nullable: false),
                    PayeeName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    PayeeAccount = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenancies", x => x.TenancyId);
                });

            migrationBuilder.CreateTable(
                name: "Bills",
                schema: "Billing",
                columns: table => new
                {
                    BillId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenancyId = table.Column<int>(type: "int", nullable: false),
                    Kind = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    DueDate = table.Column<DateOnly>(type: "date", nullable: false),
                    Period = table.Column<string>(type: "nvarchar(7)", maxLength: 7, nullable: true),
                    PayTo = table.Column<int>(type: "int", nullable: false),
                    PayeeName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    PayeeAccount = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    PaymentCode = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    PaymentModel = table.Column<string>(type: "nvarchar(2)", maxLength: 2, nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(22)", maxLength: 22, nullable: true),
                    Purpose = table.Column<string>(type: "nvarchar(35)", maxLength: 35, nullable: false),
                    CreatedByUserId = table.Column<int>(type: "int", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PaidByUserId = table.Column<int>(type: "int", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueSoonNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverdueNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bills", x => x.BillId);
                    table.ForeignKey(
                        name: "FK_Bills_Tenancies_TenancyId",
                        column: x => x.TenancyId,
                        principalSchema: "Billing",
                        principalTable: "Tenancies",
                        principalColumn: "TenancyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TenancyMembers",
                schema: "Billing",
                columns: table => new
                {
                    TenancyMemberId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TenancyId = table.Column<int>(type: "int", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false),
                    JoinedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    LeftDate = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TenancyMembers", x => x.TenancyMemberId);
                    table.ForeignKey(
                        name: "FK_TenancyMembers_Tenancies_TenancyId",
                        column: x => x.TenancyId,
                        principalSchema: "Billing",
                        principalTable: "Tenancies",
                        principalColumn: "TenancyId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BillShares",
                schema: "Billing",
                columns: table => new
                {
                    BillShareId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    BillId = table.Column<int>(type: "int", nullable: false),
                    PayerUserId = table.Column<int>(type: "int", nullable: false),
                    ReceiverUserId = table.Column<int>(type: "int", nullable: false),
                    Amount = table.Column<decimal>(type: "decimal(12,2)", nullable: false),
                    ReceiverName = table.Column<string>(type: "nvarchar(70)", maxLength: 70, nullable: false),
                    ReceiverAccount = table.Column<string>(type: "nvarchar(18)", maxLength: 18, nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    MarkedPaidAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ConfirmedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DueSoonNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    OverdueNotifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BillShares", x => x.BillShareId);
                    table.ForeignKey(
                        name: "FK_BillShares_Bills_BillId",
                        column: x => x.BillId,
                        principalSchema: "Billing",
                        principalTable: "Bills",
                        principalColumn: "BillId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_TenancyId_DueDate",
                schema: "Billing",
                table: "Bills",
                columns: new[] { "TenancyId", "DueDate" });

            migrationBuilder.CreateIndex(
                name: "IX_Bills_TenancyId_Period",
                schema: "Billing",
                table: "Bills",
                columns: new[] { "TenancyId", "Period" },
                unique: true,
                filter: "[Period] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_BillShares_BillId",
                schema: "Billing",
                table: "BillShares",
                column: "BillId");

            migrationBuilder.CreateIndex(
                name: "IX_BillShares_PayerUserId_Status",
                schema: "Billing",
                table: "BillShares",
                columns: new[] { "PayerUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_BillShares_ReceiverUserId_Status",
                schema: "Billing",
                table: "BillShares",
                columns: new[] { "ReceiverUserId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_Tenancies_ApartmentId",
                schema: "Billing",
                table: "Tenancies",
                column: "ApartmentId",
                unique: true,
                filter: "[Status] = 0");

            migrationBuilder.CreateIndex(
                name: "IX_Tenancies_LandlordUserId",
                schema: "Billing",
                table: "Tenancies",
                column: "LandlordUserId");

            migrationBuilder.CreateIndex(
                name: "IX_TenancyMembers_TenancyId_UserId",
                schema: "Billing",
                table: "TenancyMembers",
                columns: new[] { "TenancyId", "UserId" },
                unique: true,
                filter: "[LeftDate] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_TenancyMembers_UserId",
                schema: "Billing",
                table: "TenancyMembers",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BillingProfiles",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "BillShares",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "TenancyMembers",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "Bills",
                schema: "Billing");

            migrationBuilder.DropTable(
                name: "Tenancies",
                schema: "Billing");
        }
    }
}
