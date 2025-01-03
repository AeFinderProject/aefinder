using System.Threading.Tasks;
using AeFinder.Email.Dto;
using AeFinder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Emailing;

namespace AeFinder.Email;

public class BillingEmailSender:IBillingEmailSender
{
    private readonly IEmailSender _emailSender;
    private readonly AwsEmailOptions _awsEmailOptions;
    private readonly ILogger<BillingEmailSender> _logger;
    
    public BillingEmailSender(IEmailSender emailSender, IOptions<AwsEmailOptions> awsEmailOptions, 
        ILogger<BillingEmailSender> logger)
    {
        _emailSender = emailSender;
        _awsEmailOptions = awsEmailOptions.Value;
        _logger = logger;
    }
    
    public async Task SendLockBalanceSuccessfulNotificationAsync(string email, string organizationWalletAddress, decimal lockAmount)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body = 
                EmailBodyBuilder.BuildLockBalanceSuccessfulTemplate(organizationWalletAddress, lockAmount),
            Subject = "Lock organization balance successfully"
        });
    }

    public async Task SendLockBalanceFailedNotificationAsync(string email, string organizationWalletAddress,
        decimal lockAmount)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body = 
                EmailBodyBuilder.BuildLockBalanceFailedTemplate(organizationWalletAddress, lockAmount),
            Subject = "Lock organization balance failed"
        });
    }

    public async Task SendChargeBalanceSuccessfulNotificationAsync(string email, string organizationWalletAddress,
        decimal chargeAmount, decimal refundAmount)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildChargeBalanceSuccessfulTemplate(organizationWalletAddress, chargeAmount,
                    refundAmount),
            Subject = "Charge organization balance successfully"
        });
    }

    public async Task SendAutoRenewalPreDeductionSuccessfulNotificationAsync(string email, string month,
        string organizationName, decimal billAmount, decimal lockFromAmount)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildAutoRenewalPreDeductionSuccessfulTemplate(month, organizationName,
                    billAmount,lockFromAmount),
            Subject = "Auto renewal pre-deduction successfully"
        });
    }

    public async Task SendPreDeductionBalanceInsufficientNotificationAsync(string email, string month,
        string organizationName, decimal lockFromAmount, decimal organizationBalance, string organizationWalletAddress)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildPreDeductionBalanceInsufficientTemplate(month, organizationName,
                    lockFromAmount, organizationBalance, organizationWalletAddress),
            Subject = "Pre-deduction balance insufficient"
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

    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
    }
}