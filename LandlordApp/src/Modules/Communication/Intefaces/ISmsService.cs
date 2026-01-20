using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
namespace Lander.src.Modules.Communication.Intefaces;
public interface ISmsService
{
    Task<SendSmsDto> SendSmsAsync(SendSmsInputDto sendSmsInputDto);
}
