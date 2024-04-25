using System.Threading.Tasks;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.OpenIddict.Login;

public interface ILoginNewUserCreator
{
    Task<IdentityUser> CreateAsync(string userName, string password);
}