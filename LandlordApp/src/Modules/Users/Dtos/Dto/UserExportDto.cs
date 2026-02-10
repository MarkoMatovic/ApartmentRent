using Lander.src.Modules.Listings.Dtos.Dto;
using Lander.src.Modules.Roommates.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.Dto;

namespace Lander.src.Modules.Users.Dtos.Dto
{
    public class UserExportDto
    {
        public UserProfileDto UserProfile { get; set; }
        public RoommateDto? RoommateProfile { get; set; }
        public IEnumerable<ApartmentDto> ListedApartments { get; set; }
        public DateTime ExportedAt { get; set; } = DateTime.UtcNow;
    }
}
