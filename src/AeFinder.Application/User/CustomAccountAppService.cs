using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Account;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace AeFinder.User;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
[Dependency(ReplaceServices = true)]
public class CustomAccountAppService: IAccountAppService, ITransientDependency
{

    public async Task<IdentityUserDto> RegisterAsync(RegisterDto input)
    {
        return null;
    }

    public async Task SendPasswordResetCodeAsync(SendPasswordResetCodeDto input)
    {
        return;
    }

    public async Task<bool> VerifyPasswordResetTokenAsync(VerifyPasswordResetTokenInput input)
    {
        return false;
    }

    public async Task ResetPasswordAsync(ResetPasswordDto input)
    {
        return;
    }
}