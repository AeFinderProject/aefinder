using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AeFinder.ApiKeys;
using AeFinder.App.Deploy;
using AeFinder.AppResources;
using AeFinder.Assets;
using AeFinder.BlockScan;
using AeFinder.Email;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.Options;
using AeFinder.User;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using OpenIddict.Abstractions;
using Orleans;
using Shouldly;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Users;
using Xunit;

namespace AeFinder.Apps;

public class AppObliterateTests
{
    private const string AppId = "obliterate-app";
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Mock<IClusterClient> _client = new();
    private readonly Mock<IAppGrain> _app = new();
    private readonly Mock<IOrganizationAppGrain> _organizationApps = new();
    private readonly Mock<IOrganizationAppService> _organizations = new();
    private readonly Mock<ICurrentUser> _currentUser = new();
    private readonly Mock<IUserAppService> _users = new();
    private readonly Mock<IAppEmailSender> _emailSender = new();
    private readonly Mock<IOpenIddictApplicationManager> _applications = new();

    public AppObliterateTests()
    {
        var userId = Guid.NewGuid();
        _currentUser.SetupGet(x => x.Id).Returns(userId);
        _currentUser.SetupGet(x => x.IsAuthenticated).Returns(true);
        _organizations.Setup(x => x.GetUserDefaultOrganizationAsync(userId))
            .ReturnsAsync(new OrganizationUnit(_organizationId, "test-organization"));
        _client.Setup(x => x.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GetOrganizationGrainId(_organizationId), null)).Returns(_organizationApps.Object);
        _client.Setup(x => x.GetGrain<IAppGrain>(GrainIdHelper.GenerateAppGrainId(AppId), null)).Returns(_app.Object);
        _organizationApps.Setup(x => x.CheckAppIsExistAsync(AppId)).ReturnsAsync(true);
        _users.Setup(x => x.GetDefaultUserInOrganizationUnitAsync(_organizationId))
            .ReturnsAsync(new IdentityUser(userId, "owner", "owner@example.com"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Obliterate_Should_Not_Revoke_Or_Delete_When_App_Is_Not_In_Organization(bool admin)
    {
        _currentUser.Setup(x => x.IsInRole("admin")).Returns(admin);
        _organizationApps.Setup(x => x.CheckAppIsExistAsync(AppId)).ReturnsAsync(false);

        await Should.ThrowAsync<UserFriendlyException>(() => ObliterateAsync());

        VerifyNoDeletion();
    }

    [Fact]
    public async Task Obliterate_Should_Not_Revoke_Or_Delete_When_User_Has_No_Organization()
    {
        _organizations.Setup(x => x.GetUserDefaultOrganizationAsync(It.IsAny<Guid>()))
            .ReturnsAsync((OrganizationUnit)null);

        await Should.ThrowAsync<UserFriendlyException>(() => ObliterateAsync());

        VerifyNoDeletion();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Obliterate_Should_Revoke_Before_Deleting_App_And_Send_Email(bool admin)
    {
        _currentUser.Setup(x => x.IsInRole("admin")).Returns(admin);
        var application = new object();
        var operations = new List<string>();
        _applications.Setup(x => x.FindByClientIdAsync(AppId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(application);
        _applications.Setup(x => x.DeleteAsync(application, It.IsAny<CancellationToken>()))
            .Callback(() => operations.Add("revoke"))
            .Returns(ValueTask.CompletedTask);
        _app.Setup(x => x.DeleteAppAsync()).Callback(() => operations.Add("delete"))
            .Returns(Task.CompletedTask);

        await ObliterateAsync();

        operations.ShouldBe(new[] { "revoke", "delete" });
        _emailSender.Verify(x => x.SendAeIndexerDeletedNotificationAsync("owner@example.com", AppId), Times.Once);
    }

    [Fact]
    public async Task Obliterate_Should_Delete_App_When_Client_Does_Not_Exist()
    {
        await ObliterateAsync();

        _applications.Verify(x => x.FindByClientIdAsync(AppId, It.IsAny<CancellationToken>()), Times.Once);
        _applications.Verify(x => x.DeleteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _app.Verify(x => x.DeleteAppAsync(), Times.Once);
        _emailSender.Verify(x => x.SendAeIndexerDeletedNotificationAsync("owner@example.com", AppId), Times.Once);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Obliterate_Should_Propagate_Revocation_Failure_Without_Deleting_App(bool lookupFails)
    {
        var error = new InvalidOperationException("Authentication store unavailable");
        if (lookupFails)
        {
            _applications.Setup(x => x.FindByClientIdAsync(AppId, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromException<object>(error));
        }
        else
        {
            var application = new object();
            _applications.Setup(x => x.FindByClientIdAsync(AppId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(application);
            _applications.Setup(x => x.DeleteAsync(application, It.IsAny<CancellationToken>()))
                .Returns(ValueTask.FromException(error));
        }

        var actual = await Should.ThrowAsync<InvalidOperationException>(() => ObliterateAsync());

        actual.ShouldBeSameAs(error);
        _app.Verify(x => x.DeleteAppAsync(), Times.Never);
        _emailSender.Verify(x => x.SendAeIndexerDeletedNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private Task ObliterateAsync() => CreateService(_applications.Object).ObliterateAppAsync(AppId, _organizationId.ToString());

    [Fact]
    public async Task Obliterate_Should_Prevent_New_Client_Credentials_Tokens_Using_Original_DeployKey()
    {
        using var server = AppTokenTestServer.Create();
        using var client = server.CreateClient();
        using var scope = server.Services.CreateScope();
        var applications = scope.ServiceProvider.GetRequiredService<IOpenIddictApplicationManager>();
        const string deployKey = "test-deploy-key";
        var application = new OpenIddictApplicationDescriptor
        {
            ClientId = AppId,
            ClientType = OpenIddictConstants.ClientTypes.Confidential,
            Permissions =
            {
                OpenIddictConstants.Permissions.Endpoints.Token,
                OpenIddictConstants.Permissions.GrantTypes.ClientCredentials
            }
        };
        // Use the real manager to hash the secret and populate its application cache.
        await applications.CreateAsync((object)application, deployKey);
        var request = new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = AppId,
            ["client_secret"] = deployKey
        };
        using var before = await client.PostAsync("/connect/token", new FormUrlEncodedContent(request));
        before.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var token = JsonDocument.Parse(await before.Content.ReadAsStringAsync());
        token.RootElement.GetProperty("access_token").GetString().ShouldNotBeNullOrEmpty();

        await CreateService(applications).ObliterateAppAsync(AppId, _organizationId.ToString());

        using var after = await client.PostAsync("/connect/token", new FormUrlEncodedContent(request));
        after.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
        using var failure = JsonDocument.Parse(await after.Content.ReadAsStringAsync());
        failure.RootElement.GetProperty("error").GetString().ShouldBe(OpenIddictConstants.Errors.InvalidClient);
        failure.RootElement.TryGetProperty("access_token", out _).ShouldBeFalse();
        (await applications.FindByClientIdAsync(AppId)).ShouldBeNull();
    }

    private AppDeployService CreateService(IOpenIddictApplicationManager applications)
    {
        var provider = new ServiceCollection().AddLogging().AddSingleton(_currentUser.Object)
            .AddSingleton(applications).BuildServiceProvider();
        var service = ActivatorUtilities.CreateInstance<AppDeployService>(provider,
            _client.Object, Mock.Of<IBlockScanAppService>(), Mock.Of<IAppDeployManager>(), _organizations.Object,
            Options<AppDeployOptions>(), Options<CustomOrganizationOptions>(), Options<PodResourceOptions>(),
            Mock.Of<IAssetService>(), Mock.Of<IApiKeyService>(), _users.Object, _emailSender.Object,
            Mock.Of<IAppResourceLimitProvider>(), Mock.Of<IAppResourceUsageService>());
        service.LazyServiceProvider = new AbpLazyServiceProvider(provider);
        return service;
    }

    private void VerifyNoDeletion()
    {
        _applications.Verify(x => x.FindByClientIdAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        _applications.Verify(x => x.DeleteAsync(It.IsAny<object>(), It.IsAny<CancellationToken>()), Times.Never);
        _app.Verify(x => x.DeleteAppAsync(), Times.Never);
        _emailSender.Verify(x => x.SendAeIndexerDeletedNotificationAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    private static IOptionsSnapshot<T> Options<T>() where T : class, new() =>
        Mock.Of<IOptionsSnapshot<T>>(x => x.Value == new T());
}
