using Azure.Core;
using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Intefaces;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Twilio;
using Twilio.Rest.Api.V2010.Account;

namespace Lander.src.Modules.Communication.Implementation;

public class SmsService : ISmsService
{
    private readonly TwilioSettings _twilioSettings;
    private readonly CommunicationsContext _context;

    public SmsService(IOptions<TwilioSettings> twilioSettings, CommunicationsContext context)
    {
        _twilioSettings = twilioSettings.Value ?? throw new ArgumentNullException(nameof(twilioSettings));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }
    public async Task<SendSmsDto> SendSmsAsync(SendSmsInputDto sendSmsInputDto)
    {
        var message = new Message
        {
            SenderId = sendSmsInputDto.SenderId,
            ReceiverId = sendSmsInputDto.ReceiverId,
            MessageText = sendSmsInputDto.MessageText,
            SentAt = DateTime.UtcNow,
            IsRead = false,
            CreatedByGuid = Guid.NewGuid(), 
            CreatedDate = DateTime.UtcNow
        };

       
        _context.Messages.Add(message);
        await _context.SaveChangesAsync();

        TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);

        var twilioMessage = await MessageResource.CreateAsync(
            body: sendSmsInputDto.MessageText,
            from: new Twilio.Types.PhoneNumber(_twilioSettings.PhoneNumber),
            to: new Twilio.Types.PhoneNumber(sendSmsInputDto.ToPhoneNumber)
        );

        return new SendSmsDto
        {
            Success = true,
            Message = "Message sent"
        };
    }
}
