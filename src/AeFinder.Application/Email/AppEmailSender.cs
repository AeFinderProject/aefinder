using System.Threading.Tasks;
using AeFinder.Email.Dto;
using AeFinder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;

namespace AeFinder.Email;

public class AppEmailSender:IAppEmailSender, ISingletonDependency
{
    private readonly IEmailSender _emailSender;
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly ILogger<AppEmailSender> _logger;
    
    public AppEmailSender(IEmailSender emailSender, IOptions<AwsEmailOptions> awsEmailOptions, 
        ILogger<AppEmailSender> logger)
    {
        _emailSender = emailSender;
        _awsEmailOptions = awsEmailOptions.Value;
        _logger = logger;
    }
    
    public async Task SendAeIndexerCreationNotificationAsync(string email, string appName)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildAeIndexerCreatedSuccessfulTemplate(appName),
            Subject = $"AeIndexer {appName} is created"
        });
    }
    
    public async Task SendAeIndexerFreezeNotificationAsync(string email, string appId)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildAeIndexerFreezeNotificationTemplate(appId),
            Subject = $"AeIndexer {appId} is frozen"
        });
    }

    public async Task SendAeIndexerUnFreezeNotificationAsync(string email, string appId)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildAeIndexerUnFreezeNotificationTemplate(appId),
            Subject = $"AeIndexer {appId} has been unfrozen"
        });
    }
    
    public async Task SendAeIndexerDeletedNotificationAsync(string email, string appId)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildAeIndexerDeletedNotificationTemplate(appId),
            Subject = $"AeIndexer {appId} is deleted"
        });
    }
    
    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
        _logger.LogInformation($"[AppEmailSender] Send email successfully, Detail: " +
                               JsonConvert.SerializeObject(input));
    }
}