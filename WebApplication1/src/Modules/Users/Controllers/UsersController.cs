using Lander.Helpers;
using Lander.src.Modules.Users.Dtos.Dto;
using Lander.src.Modules.Users.Dtos.InputDto;
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
        public async Task<ActionResult<string>> LoginUser([FromBody]LoginUserInputDto loginUserInputDto)
        {
            return Ok(await _userInterface.LoginUserAsync(loginUserInputDto));
        }
    }
}
