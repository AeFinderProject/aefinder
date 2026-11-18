using System.Collections.Generic;
using System.Collections.Immutable;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace AeFinder.Apps;

internal static class AppTokenTestServer
{
    public static TestServer Create()
    {
        // Only persistence is substituted. Client lookup, secret hashing/validation, cache
        // invalidation, credential deletion and token issuance use real OpenIddict services.
        var applications = new Dictionary<string, OpenIddictApplicationDescriptor>();
        var store = new Mock<IOpenIddictApplicationStore<OpenIddictApplicationDescriptor>>();
        store.Setup(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => applications.GetValueOrDefault(id));
        store.Setup(x => x.FindByIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((string id, CancellationToken _) => applications.GetValueOrDefault(id));
        store.Setup(x => x.CreateAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((app, _) => applications.Add(app.ClientId, app))
            .Returns(ValueTask.CompletedTask);
        store.Setup(x => x.DeleteAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, CancellationToken>((app, _) => applications.Remove(app.ClientId))
            .Returns(ValueTask.CompletedTask);
        store.Setup(x => x.GetIdAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIddictApplicationDescriptor app, CancellationToken _) => app.ClientId);
        store.Setup(x => x.GetClientIdAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIddictApplicationDescriptor app, CancellationToken _) => app.ClientId);
        store.Setup(x => x.GetClientTypeAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIddictApplicationDescriptor app, CancellationToken _) => app.ClientType);
        store.Setup(x => x.GetClientSecretAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIddictApplicationDescriptor app, CancellationToken _) => app.ClientSecret);
        store.Setup(x => x.SetClientSecretAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<OpenIddictApplicationDescriptor, string, CancellationToken>((app, secret, _) => app.ClientSecret = secret)
            .Returns(ValueTask.CompletedTask);
        store.Setup(x => x.GetPermissionsAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((OpenIddictApplicationDescriptor app, CancellationToken _) => app.Permissions.ToImmutableArray());
        store.Setup(x => x.GetRequirementsAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmutableArray<string>.Empty);
        store.Setup(x => x.GetRedirectUrisAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmutableArray<string>.Empty);
        store.Setup(x => x.GetPostLogoutRedirectUrisAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmutableArray<string>.Empty);
        store.Setup(x => x.GetSettingsAsync(It.IsAny<OpenIddictApplicationDescriptor>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(ImmutableDictionary<string, string>.Empty);

        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(store.Object);
                services.AddAuthentication();
                services.AddOpenIddict()
                    .AddCore(options => options.SetDefaultApplicationEntity<OpenIddictApplicationDescriptor>())
                    .AddServer(options =>
                    {
                        options.SetTokenEndpointUris("/connect/token").AllowClientCredentialsFlow();
                        options.AddEphemeralEncryptionKey().AddEphemeralSigningKey();
                        options.DisableTokenStorage().DisableAuthorizationStorage().DisableScopeValidation();
                        options.UseAspNetCore().DisableTransportSecurityRequirement().EnableTokenEndpointPassthrough();
                    });
            })
            .Configure(app =>
            {
                app.UseAuthentication();
                app.Run(async context =>
                {
                    var request = context.GetOpenIddictServerRequest();
                    var identity = new ClaimsIdentity("test-client-credentials");
                    identity.AddClaim(new Claim(OpenIddictConstants.Claims.Subject, request.ClientId));
                    await context.SignInAsync(OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(identity));
                });
            }));
    }
}
