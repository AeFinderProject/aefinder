using System.Threading.Tasks;

namespace AeFinder.Email;

public interface IEmailService
{
    Task SendEmailTest(string email, string content);
}