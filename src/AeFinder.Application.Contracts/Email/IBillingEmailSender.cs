using System.Threading.Tasks;

namespace AeFinder.Email;

public interface IBillingEmailSender
{
    Task SendLockBalanceSuccessfulNotificationAsync(string email, string organizationWalletAddress, decimal lockAmount);

    Task SendLockBalanceFailedNotificationAsync(string email, string organizationWalletAddress,
        decimal lockAmount);

    Task SendChargeBalanceSuccessfulNotificationAsync(string email, string organizationWalletAddress,
        decimal chargeAmount, decimal refundAmount);

    Task SendAutoRenewalPreDeductionSuccessfulNotificationAsync(string email, string month,
        string organizationName, decimal billAmount, decimal lockFromAmount);

    Task SendPreDeductionBalanceInsufficientNotificationAsync(string email, string month,
        string organizationName, decimal lockFromAmount, decimal organizationBalance, string organizationWalletAddress);

    Task SendAeIndexerFreezeNotificationAsync(string email, string appId);
    Task SendAeIndexerUnFreezeNotificationAsync(string email, string appId);
}