namespace Lander.src.Modules.Users.Dtos.Dto;
public class UserProfileDto
{
    public int UserId { get; set; }
    public Guid UserGuid { get; set; }
    public string FirstName { get; set; }
    public string LastName { get; set; }
    public string Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? ProfilePicture { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public bool IsActive { get; set; }
    public bool IsLookingForRoommate { get; set; }
    public bool AnalyticsConsent { get; set; }
    public bool ChatHistoryConsent { get; set; }
    public bool ProfileVisibility { get; set; }
    public int? UserRoleId { get; set; }
    public string? RoleName { get; set; }
    public DateTime? CreatedDate { get; set; }
}
