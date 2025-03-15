namespace Lander.src.Modules.Listings.Dtos.InputDto;

public class ApartmentInputDto
{
    public string Title { get; set; }
    public string Description { get; set; }
    public decimal Rent { get; set; }
    public string Address { get; set; }
    public string City { get; set; }
    public string PostalCode { get; set; }
    public DateOnly AvailableFrom { get; set; }
    public DateOnly AvailableUntil { get; set; }
    public int NumberOfRooms { get; set; }
    public bool RentIncludeUtilities { get; set; }
    public List<string> ImageUrls { get; set; }
}

public class ApartmentImageInputDto
{
    public string ImageUrl { get; set; }
}