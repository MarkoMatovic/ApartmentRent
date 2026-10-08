namespace Lander.src.Modules.Users.Dtos.Dto;
public class UserRegistrationDto
{
    public int UserId { get; set; }
    public string UserGuid { get; set; } = null!;
    public string FirstName { get; set; } = null!;
    public string LastName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public int? UserRoleId { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public bool IsLookingForRoommate { get; set; }
}
