using System.Threading.Tasks;
using AeFinder.User.Dto;
using Volo.Abp.Identity;

namespace AeFinder.User;

public interface IUserAppService
{
    Task<IdentityUserDto> RegisterUserWithOrganization(RegisterUserWithOrganizationInput input);

    Task RegisterAppAuthentication(string appId, string deployKey);

    Task<IdentityUserDto> GetUserInfoAsync();

    Task ResetPasswordAsync(string newPassword);

    Task<string> GetClientDisplayNameAsync(string clientId);
}