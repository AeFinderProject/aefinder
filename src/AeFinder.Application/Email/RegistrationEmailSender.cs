using System.Threading.Tasks;
using AeFinder.Email.Dto;
using AeFinder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;

namespace AeFinder.Email;

public class RegistrationEmailSender:IRegistrationEmailSender, ISingletonDependency
{
    private readonly IEmailSender _emailSender;
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly ILogger<RegistrationEmailSender> _logger;
    
    public RegistrationEmailSender(IEmailSender emailSender, IOptions<AwsEmailOptions> awsEmailOptions, 
        ILogger<RegistrationEmailSender> logger)
    {
        _emailSender = emailSender;
        _awsEmailOptions = awsEmailOptions.Value;
        _logger = logger;
    }
    
    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
    }
}