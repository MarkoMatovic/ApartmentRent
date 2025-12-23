using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Intefaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Lander.src.Modules.Communication.Contollers
{
    [Route(ApiActionsV1.Sms)]
    [ApiController]
    public class SmsController : ControllerBase
    {
        private readonly ISmsService _smsService;

        public SmsController(ISmsService smsService)
        {
            _smsService = smsService ?? throw new ArgumentNullException(nameof(smsService));
        }
        [HttpPost(ApiActionsV1.SendSms, Name = nameof(ApiActionsV1.SendSms))]
        public async Task<ActionResult<SendSmsDto>> SendSms([FromBody] SendSmsInputDto sendSmsInputDto)
        {
            var response = await _smsService.SendSmsAsync(sendSmsInputDto);

            if (response.Success)
            {
                return Ok(response);
            }

            return BadRequest(response);
        }

    }
}
