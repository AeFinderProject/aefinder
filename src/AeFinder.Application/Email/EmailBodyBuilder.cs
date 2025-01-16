namespace AeFinder.Email;

public class EmailBodyBuilder
{
    public static string BuildRegistrationSuccessfulTemplate(string userName)
    {
        return $@" User {userName} Registration Successful";
    }

    public static string BuildAeIndexerCreatedSuccessfulTemplate(string appName)
    {
        return $@" AeIndexer {appName} created successfully";
    }

    public static string BuildEmailVerifyLinkTemplate(string verifyCode)
    {
        return $@" Please click the link http://www.aefinder.io/email/{verifyCode} to verify your email.";
    }

    public static string BuildLockBalanceSuccessfulTemplate(decimal lockAmount,
        string transactionId)
    {
        return
            $@" Your account has successfully locked a pre-deduction amount of ${lockAmount}, Transaction id: {transactionId}.";
    }

    public static string BuildLockBalanceFailedTemplate(decimal lockAmount)
    {
        return $@" Failed to lock a pre-deduction amount of ${lockAmount} in your account.";
    }

    public static string BuildChargeBalanceSuccessfulTemplate(decimal chargeAmount,string transactionId)
    {
        return
            $@" Successfully charged ${chargeAmount} from the locked balance in your account, Transaction id: {transactionId}.";
    }

    public static string BuildAutoRenewalPreDeductionFailedTemplate(string month,decimal billAmount, decimal lockFromAmount)
    {
        return $@" The total assets bill for {month} of your account amounts to ${billAmount}. A pre-deduction of ${lockFromAmount} is failed. Please check your organization balance.";
    }

    public static string BuildPreDeductionBalanceInsufficientTemplate(string month,decimal lockFromAmount,decimal organizationBalance)
    {
        return
            $@" Your account's pre-deduction amount for {month} is ${lockFromAmount}, while the current organization account balance is ${organizationBalance}. The account balance is insufficient; please top up promptly to avoid the risk of freezing some resources.";
    }

    public static string BuildAeIndexerFreezeNotificationTemplate(string appId)
    {
        return $@" The AeIndexer {appId} is frozen.";
    }

    public static string BuildAeIndexerUnFreezeNotificationTemplate(string appId)
    {
        return $@" The AeIndexer {appId} has been unfrozen.";
    }

    public static string BuildAeIndexerDeletedNotificationTemplate(string appName)
    {
        return $@" AeIndexer {appName} has been deleted.";
    }
}