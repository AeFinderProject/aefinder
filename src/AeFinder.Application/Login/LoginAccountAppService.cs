using System.Net.Http;
using System.Threading.Tasks;
using AeFinder.Login.Dto;
using AeFinder.OpenIddict;
using AeFinder.OpenIddict.Login;
using AeFinder.Option;
using EAeFinder.Login.Dto;
using IdentityModel.Client;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace AeFinder.Login;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class LoginAccountAppService : AeFinderAppService, ILoginAccountAppService, ISingletonDependency
{
    private readonly ILoginNewUserCreator _loginNewUserCreator;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly AuthOption _authOption;
    private readonly IOpenIddictApplicationManager _applicationManager;

    public LoginAccountAppService(ILoginNewUserCreator loginNewUserCreator,
        IHttpClientFactory httpClientFactory,
        IOptionsMonitor<AuthOption> authOption, IOpenIddictApplicationManager applicationManager)
    {
        _loginNewUserCreator = loginNewUserCreator;
        _httpClientFactory = httpClientFactory;
        _applicationManager = applicationManager;
        _authOption = authOption.CurrentValue;
    }

    public async Task<IdentityUserDto> RegisterAsync(RegisterWithNameInput input)
    {
        var identityUser = await _loginNewUserCreator.CreateAsync(input.UserName, input.Password);
        return ObjectMapper.Map<IdentityUser, IdentityUserDto>(identityUser);
    }

    public async Task<string> RequestTokenByPasswordAsync(RequestTokenByPasswordInput input)
    {
        return (await RequestAuthServerLoginByPasswordAsync(input.UserName, input.Password))?.Raw;
    }

    public async Task<string> RefreshTokenAsync(RefreshTokenInput input)
    {
        return (await RequestAuthServerRefreshAsync(input.RefreshToken)).Raw;
    }

    protected virtual async Task<TokenResponse> RequestAuthServerLoginByPasswordAsync(string userName, string password)
    {
        var client = _httpClientFactory.CreateClient(LoginConsts.IdentityServerHttpClientName);

        var request = new TokenRequest
        {
            Address = $"{_authOption.TokenConnectUrl}/connect/token",
            GrantType = LoginConsts.GrantType,

            ClientId = _authOption.ClientId,
            ClientSecret = _authOption.ClientSecret,

            Parameters =
            {
                { "name", userName },
                { "password", password }
            }
        };

        request.Headers.Add(GetTenantHeaderName(), CurrentTenant.Id?.ToString());

        var res = await client.RequestTokenAsync(request);
        return res;
    }

    protected virtual async Task<TokenResponse> RequestAuthServerRefreshAsync(string refreshToken)
    {
        var client = _httpClientFactory.CreateClient(LoginConsts.IdentityServerHttpClientName);

        var request = new RefreshTokenRequest
        {
            Address = $"{_authOption.TokenConnectUrl}/connect/token",

            ClientId = _authOption.ClientId,
            ClientSecret = _authOption.ClientSecret,

            RefreshToken = refreshToken
        };

        request.Headers.Add(GetTenantHeaderName(), CurrentTenant.Id?.ToString());

        return await client.RequestRefreshTokenAsync(request);
    }

    protected virtual string GetTenantHeaderName()
    {
        return "__tenant";
    }
}