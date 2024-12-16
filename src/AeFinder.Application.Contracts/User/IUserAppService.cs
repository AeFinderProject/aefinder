using System.Threading.Tasks;
using AeFinder.User.Dto;
using Volo.Abp.Identity;

namespace AeFinder.User;

public interface IUserAppService
{
    Task<IdentityUserDto> RegisterUserWithOrganization(RegisterUserWithOrganizationInput input);

    Task RegisterAppAuthentication(string appId, string deployKey);
    Task DeleteAppAuthentication(string appId);
    Task DeleteAppRelatedTokenData(string appId);

    Task<IdentityUserExtensionDto> GetUserInfoAsync();

    Task ResetPasswordAsync(string userName, string newPassword);

    Task<string> GetClientDisplayNameAsync(string clientId);

    Task<IdentityUserExtensionDto> BindUserWalletAsync(BindUserWalletInput input);

    Task RegisterAsync(RegisterUserInput input);
    Task RegisterConfirmAsync(string code);
    Task ResendRegisterEmailAsync(ResendEmailInput input);
}