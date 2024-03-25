using System.Threading.Tasks;
using AeFinder.Login.Dto;
using EAeFinder.Login.Dto;
using Volo.Abp.Identity;

namespace AeFinder.Login;

public interface ILoginAccountAppService
{
    Task<IdentityUserDto> RegisterAsync(RegisterWithNameInput input);

    Task<string> RequestTokenByPasswordAsync(RequestTokenByPasswordInput input);

    Task<string> RefreshTokenAsync(RefreshTokenInput input);
}