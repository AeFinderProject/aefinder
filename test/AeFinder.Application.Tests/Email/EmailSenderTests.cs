using System;
using System.Threading.Tasks;
using Shouldly;
using Xunit;

namespace AeFinder.Email;

public class EmailSenderTests: AeFinderApplicationAppTestBase
{
    private readonly IBillingEmailSender _billingEmailSender;
    
    public EmailSenderTests()
    {
        _billingEmailSender = GetRequiredService<IBillingEmailSender>();
    }

    [Fact]
    public async Task BillingEmailSenderTest()
    {
        try
        {
            await _billingEmailSender.SendLockBalanceSuccessfulNotificationAsync("949459091@qq.com", "abcdefg",
                22);
        }
        catch (Exception e)
        {
            e.Message.ShouldContain("Failure sending mail.");
        }
    }
}