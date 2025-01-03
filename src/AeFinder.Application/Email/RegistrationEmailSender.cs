using AeFinder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Emailing;

namespace AeFinder.Email;

public class RegistrationEmailSender:IRegistrationEmailSender
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
    
    
}