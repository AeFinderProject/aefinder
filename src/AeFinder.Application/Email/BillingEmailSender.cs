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
                EmailBodyBuilder.BuildLockBalanceSuccessfulTemplate(organizationWalletAddress, lockAmount,
                    transactionId),
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
        decimal chargeAmount, string transactionId)
    {
        await SendEmailAsync(new SendEmailInput
        {
            From = _awsEmailOptions.From,
            To = email,
            Body =
                EmailBodyBuilder.BuildChargeBalanceSuccessfulTemplate(organizationWalletAddress, chargeAmount,transactionId),
            Subject = "Charge organization balance successfully"
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
                EmailBodyBuilder.BuildAutoRenewalPreDeductionFailedTemplate(month, organizationName,
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
                EmailBodyBuilder.BuildPreDeductionBalanceInsufficientTemplate(month, organizationName,
                    lockFromAmount, organizationBalance, organizationWalletAddress),
            Subject = "Pre-deduction balance insufficient"
        });
    }

    

    private async Task SendEmailAsync(SendEmailInput input)
    {
        await _emailSender.QueueAsync(input.From, input.To, input.Subject, input.Body, false);
    }
}