using Lander.src.Common;
using Lander.src.Modules.Listings.Interfaces;

namespace Lander.src.Modules.Listings.Implementation;

public class ApartmentUserDeletedHandler : IUserDeletedHandler
{
    private readonly IApartmentService _apartmentService;
    public ApartmentUserDeletedHandler(IApartmentService apartmentService) => _apartmentService = apartmentService;
    public Task HandleAsync(int userId) => _apartmentService.DeleteApartmentsByLandlordIdAsync(userId);
}
