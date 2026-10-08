using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddFeaturesJsonAndVectorSearch : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_City_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments");

            // The boolean feature columns are dropped below, but IX_Apartments_Filters_Boolean (created by
            // 20251219122049_AddPerformanceIndexes) still depends on them. On existing databases that
            // index had been removed by hand, so the drop worked there; on a brand-new database SQL Server
            // refuses ("The index ... is dependent on column 'IsFurnished'"). Drop whatever non-primary
            // index still references these columns first - looked up from the catalog, so it is a no-op
            // where none remain.
            migrationBuilder.Sql(@"
DECLARE @sql nvarchar(max) = N'';
SELECT @sql += N'DROP INDEX ' + QUOTENAME(i.name) + N' ON [Listings].[Apartments];' + CHAR(10)
FROM sys.indexes i
WHERE i.object_id = OBJECT_ID(N'[Listings].[Apartments]')
  AND i.is_primary_key = 0 AND i.name IS NOT NULL
  AND EXISTS (SELECT 1 FROM sys.index_columns ic
              JOIN sys.columns c ON c.object_id = ic.object_id AND c.column_id = ic.column_id
              WHERE ic.object_id = i.object_id AND ic.index_id = i.index_id
                AND c.name IN (N'HasAirCondition', N'HasBalcony', N'HasElevator', N'HasInternet',
                               N'HasParking', N'IsFurnished', N'IsPetFriendly', N'IsSmokingAllowed'));
IF LEN(@sql) > 0 EXEC sp_executesql @sql;");

            migrationBuilder.DropColumn(
                name: "HasAirCondition",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasBalcony",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasElevator",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasInternet",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "HasParking",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsFurnished",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsPetFriendly",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "IsSmokingAllowed",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(10,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(18,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "DescriptionEmbedding",
                schema: "Listings",
                table: "Apartments",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Features",
                schema: "Listings",
                table: "Apartments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "{}");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_ApartmentType",
                schema: "Listings",
                table: "Apartments",
                column: "ApartmentType");

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_IsImmediatelyAvailable",
                schema: "Listings",
                table: "Apartments",
                column: "IsImmediatelyAvailable");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Apartments_ApartmentType",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropIndex(
                name: "IX_Apartments_IsImmediatelyAvailable",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "DescriptionEmbedding",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.DropColumn(
                name: "Features",
                schema: "Listings",
                table: "Apartments");

            migrationBuilder.AlterColumn<decimal>(
                name: "Price",
                schema: "Listings",
                table: "Apartments",
                type: "decimal(18,2)",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "decimal(10,2)",
                oldNullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "HasAirCondition",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasBalcony",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasElevator",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasInternet",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "HasParking",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsFurnished",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsPetFriendly",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsSmokingAllowed",
                schema: "Listings",
                table: "Apartments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateIndex(
                name: "IX_Apartments_City_IsActive_IsDeleted",
                schema: "Listings",
                table: "Apartments",
                columns: new[] { "City", "IsActive", "IsDeleted" });
        }
    }
}
