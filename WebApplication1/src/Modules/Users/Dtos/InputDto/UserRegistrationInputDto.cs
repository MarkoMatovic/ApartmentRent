namespace Lander.src.Modules.Users.Dtos.InputDto
{
    public class UserRegistrationInputDto
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PhoneNumber { get; set; }
        public string? ProfilePicture { get; set; }
        public DateTime CreatedDate { get; set; }
    }

    public class LoginUserInputDto
    {
        public string Email { get; set; }
        public string Password { get; set; }
    }
}
