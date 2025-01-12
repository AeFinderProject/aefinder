using System.Threading.Tasks;
using AeFinder.Email.Dto;
using AeFinder.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Emailing;

namespace AeFinder.Email;

public class BillingEmailSender:IBillingEmailSender, ISingletonDependency
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

    public async Task SendOrderPayFailedNotificationAsync(string email, string orderId, string orderFailedText)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body = orderFailedText,
            Subject = $"Order {orderId} pay failed"
        });
    }

    public async Task SendLockBalanceSuccessfulNotificationAsync(string email, string organizationWalletAddress,
        decimal lockAmount, string transactionId)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildLockBalanceSuccessfulTemplate(lockAmount,
                    transactionId),
            Subject = "Lock balance successfully"
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
                EmailBodyBuilder.BuildLockBalanceFailedTemplate(lockAmount),
            Subject = "Lock balance failed"
        });
    }

    public async Task SendChargeBalanceSuccessfulNotificationAsync(string email, string organizationWalletAddress,
        decimal chargeAmount, string transactionId)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildChargeBalanceSuccessfulTemplate(chargeAmount,transactionId),
            Subject = "Charge balance successfully"
        });
    }

    public async Task SendAutoRenewalPreDeductionFailedNotificationAsync(string email, string month,
        string organizationName, decimal billAmount, decimal lockFromAmount)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildAutoRenewalPreDeductionFailedTemplate(month, 
                    billAmount, lockFromAmount),
            Subject = "Auto renewal pre-deduction failed"
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
                EmailBodyBuilder.BuildPreDeductionBalanceInsufficientTemplate(month, 
                    lockFromAmount, organizationBalance),
            Subject = "Pre-deduction balance insufficient"
        });
    }

    public async Task SendTestEmail(string email, string content)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body = content,
            Subject = "AeFinder Email Test"
        });
    }



    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
    }
}