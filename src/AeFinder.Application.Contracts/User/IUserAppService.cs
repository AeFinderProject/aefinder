using System.Threading.Tasks;
using AeFinder.User.Dto;
using Volo.Abp.Identity;

namespace AeFinder.User;

public interface IUserAppService
{
    Task<IdentityUserDto> RegisterUserWithOrganization(RegisterUserWithOrganizationInput input);

    Task RegisterAppAuthentication(string appId, string deployKey);

    Task<IdentityUserExtensionDto> GetUserInfoAsync();

    Task ResetPasswordAsync(string userName, string newPassword);

    Task<string> GetClientDisplayNameAsync(string clientId);

    Task<IdentityUserExtensionDto> BindUserWalletAsync(BindUserWalletInput input);
}