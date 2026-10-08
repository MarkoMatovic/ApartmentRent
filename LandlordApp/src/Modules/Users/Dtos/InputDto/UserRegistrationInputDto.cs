namespace Lander.src.Modules.Users.Dtos.InputDto
{
    public class UserRegistrationInputDto
    {
        public string FirstName { get; set; } = null!;
        public string LastName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; } = null!;
        public string? ProfilePicture { get; set; }
        public DateTime CreatedDate { get; set; }
    }
    public class LoginUserInputDto
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
    public class DeactivateUserInputDto
    {
        public Guid UserGuid { get; set; }
    }
    public class ReactivateUserInputDto
    {
        public Guid UserGuid { get; set; }
    }
    public class DeleteUserInputDto
    {
        public Guid UserGuid { get; set; }
    }
    public class ChangePasswordInputDto
    {
        public Guid UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
    public class UpdateRoommateStatusInputDto
    {
        public Guid UserGuid { get; set; }
        public bool IsLookingForRoommate { get; set; }
    }
    public class UserProfileUpdateInputDto
    {
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        // Email is intentionally excluded — changes require a dedicated verify-email flow.
        public string? PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public bool? HasPersonalAnalytics { get; set; }
        public bool? HasLandlordAnalytics { get; set; }
    }
}
