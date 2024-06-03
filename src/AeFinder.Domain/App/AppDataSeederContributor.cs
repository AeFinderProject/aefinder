using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Volo.Abp.Data;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;
using Volo.Abp.Uow;
using IdentityRole = Volo.Abp.Identity.IdentityRole;

namespace AeFinder.App;

public class AppDataSeederContributor: IDataSeedContributor, ITransientDependency
{
    private readonly IConfiguration _configuration;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IIdentityRoleRepository _roleRepository;
    private readonly ILookupNormalizer _lookupNormalizer;

    public AppDataSeederContributor(IConfiguration configuration, IdentityUserManager identityUserManager,
        IIdentityRoleRepository roleRepository, ILookupNormalizer lookupNormalizer)
    {
        _configuration = configuration;
        _identityUserManager = identityUserManager;
        _roleRepository = roleRepository;
        _lookupNormalizer = lookupNormalizer;
    }

    [UnitOfWork]
    public async Task SeedAsync(DataSeedContext context)
    {
        await SeedAdminUserAsync();
        await SeedAppAdminRoleAsync();
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
    
    public async Task SeedAppAdminRoleAsync()
    {
        var normalizedRoleName = _lookupNormalizer.NormalizeName("appAdmin");
        var appAdminRole = await _roleRepository.FindByNormalizedNameAsync(normalizedRoleName);
        
        if (appAdminRole == null)
        {
            appAdminRole = new IdentityRole(Guid.NewGuid(), "appAdmin")
            {
                IsStatic = true,
                IsPublic = true
            };
            await _roleRepository.InsertAsync(appAdminRole);
        }
    }
    
    
}