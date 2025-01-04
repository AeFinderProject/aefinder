using System.Threading.Tasks;

namespace AeFinder.Email;

public interface IRegistrationEmailSender
{
    Task SendAeIndexerCreationNotificationAsync(string email, string appName);
}