using Azure.Core;
using Lander.Helpers;
using Lander.src.Modules.Communication.Dtos.Dto;
using Lander.src.Modules.Communication.Dtos.InputDto;
using Lander.src.Modules.Communication.Interfaces;
using Lander.src.Modules.Communication.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Twilio;
using Twilio.Rest.Api.V2010.Account;
namespace Lander.src.Modules.Communication.Implementation;
public class SmsService : ISmsService
{
    private static readonly ResiliencePipeline _smsPipeline = new ResiliencePipelineBuilder()
        .AddRetry(new Polly.Retry.RetryStrategyOptions
        {
            MaxRetryAttempts = 3,
            Delay = TimeSpan.FromSeconds(2),
            BackoffType = DelayBackoffType.Exponential,
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .AddCircuitBreaker(new Polly.CircuitBreaker.CircuitBreakerStrategyOptions
        {
            FailureRatio = 0.5,
            SamplingDuration = TimeSpan.FromSeconds(30),
            MinimumThroughput = 5,
            BreakDuration = TimeSpan.FromMinutes(1),
            ShouldHandle = new PredicateBuilder().Handle<Exception>()
        })
        .Build();

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
        var transaction = await _context.BeginTransactionAsync();
        try
        {
            _context.Messages.Add(message);
            await _context.SaveEntitiesAsync();
            await _context.CommitTransactionAsync(transaction);
        }
        catch
        {
            _context.RollBackTransaction();
            throw;
        }
        TwilioClient.Init(_twilioSettings.AccountSid, _twilioSettings.AuthToken);
        var twilioMessage = await _smsPipeline.ExecuteAsync(async ct =>
            await MessageResource.CreateAsync(
                body: sendSmsInputDto.MessageText,
                from: new Twilio.Types.PhoneNumber(_twilioSettings.PhoneNumber),
                to: new Twilio.Types.PhoneNumber(sendSmsInputDto.ToPhoneNumber)));
        return new SendSmsDto
        {
            Success = true,
            Message = "Message sent"
        };
    }
}
