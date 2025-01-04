using System.Threading.Tasks;

namespace AeFinder.Email;

public interface IAppEmailSender
{
    Task SendAeIndexerCreationNotificationAsync(string email, string appName);
    Task SendAeIndexerFreezeNotificationAsync(string email, string appId);
    Task SendAeIndexerUnFreezeNotificationAsync(string email, string appId);
    Task SendAeIndexerDeletedNotificationAsync(string email, string appName);
}