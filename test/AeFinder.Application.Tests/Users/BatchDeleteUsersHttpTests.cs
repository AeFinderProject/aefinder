using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text;
using System.Threading.Tasks;
using AeFinder.Controllers;
using AeFinder.User;
using AeFinder.User.Dto;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Modularity;
using Xunit;

namespace AeFinder.Users;

public class BatchDeleteUsersHttpTests
{
    [Theory]
    [InlineData(null, HttpStatusCode.Unauthorized)]
    [InlineData("user", HttpStatusCode.Forbidden)]
    [InlineData("admin", HttpStatusCode.OK)]
    public async Task Require_Admin_Role(string role, HttpStatusCode expectedStatus)
    {
        var service = new Mock<IUserAppService>();
        service.Setup(x => x.BatchDeleteAsync(It.IsAny<BatchDeleteUsersInput>()))
            .ReturnsAsync(new BatchDeleteUsersResultDto());
        using var server = CreateServer(service.Object);
        using var client = server.CreateClient();
        if (role != null)
        {
            client.DefaultRequestHeaders.Add("X-Test-Role", role);
        }

        var userId = Guid.NewGuid();
        var response = await client.PostAsJsonAsync("/api/users/batch-delete", new
        {
            userIds = new[] { userId }
        });

        response.StatusCode.ShouldBe(expectedStatus);
        service.Verify(x => x.BatchDeleteAsync(It.Is<BatchDeleteUsersInput>(
                input => input.UserIds.Count == 1 && input.UserIds[0] == userId)),
            role == "admin" ? Times.Once() : Times.Never());
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("{\"userIds\":null}")]
    [InlineData("{\"userIds\":[]}")]
    [InlineData("{\"userIds\":[\"00000000-0000-0000-0000-000000000000\"]}")]
    [InlineData("{\"userIds\":[\"invalid-guid\"]}")]
    public async Task Reject_Invalid_Request(string body)
    {
        var service = new Mock<IUserAppService>();
        using var server = CreateServer(service.Object);
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "admin");

        var response = await client.PostAsync("/api/users/batch-delete",
            new StringContent(body, Encoding.UTF8, "application/json"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        service.Verify(x => x.BatchDeleteAsync(It.IsAny<BatchDeleteUsersInput>()), Times.Never());
    }

    [Fact]
    public async Task Reject_Over_200_Ids()
    {
        var service = new Mock<IUserAppService>();
        using var server = CreateServer(service.Object);
        using var client = server.CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "admin");

        var response = await client.PostAsJsonAsync("/api/users/batch-delete", new
        {
            userIds = System.Linq.Enumerable.Repeat(Guid.NewGuid(), 201)
        });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        service.Verify(x => x.BatchDeleteAsync(It.IsAny<BatchDeleteUsersInput>()), Times.Never());
    }

    private static TestServer CreateServer(IUserAppService service)
    {
        return new TestServer(new WebHostBuilder()
            .ConfigureServices(services =>
            {
                services.AddSingleton(service);
                services.ReplaceConfiguration(new ConfigurationBuilder().Build());
                services.AddAuthentication("Test")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthenticationHandler>("Test", _ => { });
                services.AddAuthorization(options => options.AddPolicy("OnlyAdminAccess",
                    policy => policy.RequireRole("admin")));
                services.AddApplication<BatchDeleteUsersHttpTestModule>();
                services.AddControllers().AddApplicationPart(typeof(UserController).Assembly);
            })
            .Configure(app =>
            {
                app.InitializeApplication();
                app.UseRouting();
                app.UseAuthentication();
                app.UseAuthorization();
                app.UseEndpoints(endpoints => endpoints.MapControllers());
            }));
    }

    private class TestAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger, UrlEncoder encoder) : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Test-Role", out var role))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var identity = new ClaimsIdentity(new[]
            {
                // A user named admin without the role must still be denied.
                new Claim(ClaimTypes.Name, "admin"),
                new Claim(ClaimTypes.Role, role.ToString())
            }, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(
                new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name)));
        }
    }
}

[DependsOn(typeof(AeFinderHttpApiModule))]
public class BatchDeleteUsersHttpTestModule : AbpModule
{
}
