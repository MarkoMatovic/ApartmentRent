namespace Lander.src.Modules.Listings.Models;

// Values and names mirror the frontend enum (front-land/src/shared/types/apartment.ts)
// and the values the frontend has historically written to the database — do not
// renumber without a data migration on Apartments.ApartmentType.
public enum ApartmentType
{
    Studio = 0,
    OneRoom = 1,
    TwoRoom = 2,
    ThreeRoom = 3,
    FourRoom = 4,
    House = 5
}
