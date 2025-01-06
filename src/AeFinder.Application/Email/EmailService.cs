using System.Threading.Tasks;

namespace AeFinder.Email;

public class EmailService : AeFinderAppService, IEmailService
{
    private readonly IBillingEmailSender _billingEmailSender;

    public EmailService(IBillingEmailSender billingEmailSender)
    {
        _billingEmailSender = billingEmailSender;
    }

    public async Task SendEmailTest(string email)
    {
        await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync(email, "testAddress",
            22,"aa123");
    }
}