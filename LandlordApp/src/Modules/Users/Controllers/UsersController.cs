using Lander.Helpers;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
using Lander.src.Modules.Users.Implementation.UserImplementation;
using Lander.src.Modules.Users.Interfaces.UserInterface;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Users.Controllers
{
    [Route(ApiActionsV1.Auth)]
    [ApiController]
    public class UsersController : ControllerBase
    {
        #region Properties
        private readonly IUserInterface _userInterface;
        #endregion
        #region Constructors
        public UsersController(IUserInterface userInterface)
        {
            _userInterface = userInterface;
        }
        #endregion

        [HttpPost(ApiActionsV1.Register, Name = nameof(ApiActionsV1.Register))]
        public async Task<ActionResult<UserRegistrationDto>> RegisterUser([FromBody] UserRegistrationInputDto userRegistrationInputDto)
        {
            return Ok(await _userInterface.RegisterUserAsync(userRegistrationInputDto));
        }
        [HttpPost(ApiActionsV1.Login, Name = nameof(ApiActionsV1.Login))]
        public async Task<ActionResult<string>> LoginUser([FromBody] LoginUserInputDto loginUserInputDto)
        {
            return Ok(await _userInterface.LoginUserAsync(loginUserInputDto));
        }
        [HttpPost(ApiActionsV1.Logout, Name = nameof(ApiActionsV1.Logout))]
        public async Task<ActionResult> Logout()
        {
            await _userInterface.LogoutUserAsync();
            return Ok(new { Message = "Logged out successfully" });
        }
        [HttpPost(ApiActionsV1.ChangePassword, Name = nameof(ApiActionsV1.ChangePassword))]
        public async Task<ActionResult> ChangePassword([FromBody] ChangePasswordInputDto changePasswordInputDto)
        {
            await _userInterface.ChangePasswordAsync(changePasswordInputDto);
            return Ok();
        }
        [HttpDelete(ApiActionsV1.DeleteUser, Name = nameof(ApiActionsV1.DeleteUser))]
        public async Task<ActionResult<bool>> DeleteUser(DeleteUserInputDto deleteUserInputDto)
        {
            await _userInterface.DeleteUserAsync(deleteUserInputDto);
            return Ok(true);
        }
        [HttpPost(ApiActionsV1.DeactivateUser, Name = nameof(ApiActionsV1.DeactivateUser))]
        public async Task<IActionResult> DeactivateUser([FromBody] DeactivateUserInputDto deactivateUserInputDto)
        {
            await _userInterface.DeactivateUserAsync(deactivateUserInputDto);
            return Ok("User deactivated successfully.");
        }

        [HttpPost(ApiActionsV1.ReactivateUser, Name = nameof(ApiActionsV1.ReactivateUser))]
        public async Task<IActionResult> ReactivateUser([FromBody] ReactivateUserInputDto reactivateUserInputDto)
        {
            await _userInterface.ReactivateUserAsync(reactivateUserInputDto);
            return Ok("User reactivated successfully.");
        }
    }
}
