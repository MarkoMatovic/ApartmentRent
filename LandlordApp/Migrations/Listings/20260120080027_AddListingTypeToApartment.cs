using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Lander.Migrations.Listings
{
    /// <inheritdoc />
    public partial class AddListingTypeToApartment : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // This migration used to be EMPTY: the column was added to existing databases by hand, so a
            // brand-new database never got it and the later IX_Apartments_ListingType migration failed
            // ("Column name 'ListingType' does not exist"). Guarded so it adds the column on a fresh
            // database and does nothing where it already exists. Matches the existing column:
            // int NOT NULL DEFAULT 1 (ListingType.Rent).
            migrationBuilder.Sql(@"
IF COL_LENGTH(N'[Listings].[Apartments]', N'ListingType') IS NULL
    ALTER TABLE [Listings].[Apartments] ADD [ListingType] int NOT NULL CONSTRAINT [DF_Apartments_ListingType] DEFAULT (1);");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {

        }
    }
}
