using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Guids;
using Volo.Abp.Identity;
using Volo.Abp.MultiTenancy;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.OpenIddict.Login;

public class LoginNewUserCreator : ILoginNewUserCreator, ITransientDependency
{
    private readonly ICurrentTenant _currentTenant;
    private readonly IGuidGenerator _guidGenerator;
    private readonly IOptions<IdentityOptions> _identityOptions;
    private readonly IdentityUserManager _identityUserManager;

    public LoginNewUserCreator(ICurrentTenant currentTenant, IOptions<IdentityOptions> identityOptions, IdentityUserManager identityUserManager, IGuidGenerator guidGenerator)
    {
        _currentTenant = currentTenant;
        _identityOptions = identityOptions;
        _identityUserManager = identityUserManager;
        _guidGenerator = guidGenerator;
    }

    public async Task<IdentityUser> CreateAsync(string userName, string password)
    {
        var user = await _identityUserManager.FindByNameAsync(userName);
        if (user != null)
        {
            throw new UserFriendlyException("user already exists");
        }

        await _identityOptions.SetAsync();

        var identityUser = new IdentityUser(_guidGenerator.Create(), userName, await GenerateEmailAsync(), _currentTenant.Id);

        (await _identityUserManager.CreateAsync(identityUser)).CheckErrors();


        if (!string.IsNullOrEmpty(password))
        {
            (await _identityUserManager.AddPasswordAsync(identityUser, password)).CheckErrors();
        }

        (await _identityUserManager.AddDefaultRolesAsync(identityUser)).CheckErrors();

        return identityUser;
    }

    protected virtual Task<string> GenerateEmailAsync()
    {
        return Task.FromResult($"{Guid.NewGuid()}@fake-email.com");
    }
}