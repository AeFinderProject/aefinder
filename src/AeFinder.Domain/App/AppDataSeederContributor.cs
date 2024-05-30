using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Uow;

namespace AeFinder.App;

public class AppDataSeederContributor: IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IdentityUserManager _identityUserManager;

    public AppDataSeederContributor(IConfiguration configuration, IdentityUserManager identityUserManager)
    {
        _configuration = configuration;
        _identityUserManager = identityUserManager;
    }
    
    [UnitOfWork]
    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedAdminUserAsync();
    }

    private async Task SeedAdminUserAsync()
    {
        var adminUser = await _identityUserManager.FindByNameAsync("admin");
        if (adminUser != null)
        {
            var adminPassword = _configuration["App:AdminPassword"];
            var token = await _identityUserManager.GeneratePasswordResetTokenAsync(adminUser);
            var result = await _identityUserManager.ResetPasswordAsync(adminUser, token, adminPassword);
            if (!result.Succeeded)
            {
                throw new Exception("Failed to set admin password: " + result.Errors.Select(e => e.Description).Aggregate((errors, error) => errors + ", " + error));
            }
        }
    }
    
}