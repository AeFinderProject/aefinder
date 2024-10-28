using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Volo.Abp.Domain.Repositories;

namespace AeFinder.User;

public class AppUserManager: UserManager<AppIdentityUser>
{
    private readonly IRepository<AppIdentityUser, Guid> _userRepository;

    public AppUserManager(
        IUserStore<AppIdentityUser> store,
        IOptions<IdentityOptions> optionsAccessor,
        IPasswordHasher<AppIdentityUser> passwordHasher,
        IEnumerable<IUserValidator<AppIdentityUser>> userValidators,
        IEnumerable<IPasswordValidator<AppIdentityUser>> passwordValidators,
        ILookupNormalizer keyNormalizer,
        IdentityErrorDescriber errors,
        IServiceProvider services,
        ILogger<UserManager<AppIdentityUser>> logger,
        IRepository<AppIdentityUser, Guid> userRepository)
        : base(store, optionsAccessor, passwordHasher, userValidators, passwordValidators, keyNormalizer, errors, services, logger)
    {
        _userRepository = userRepository;
    }

    public async Task<AppIdentityUser> FindByAElfAddressAsync(string aElfAddress)
    {
        return await _userRepository.FirstOrDefaultAsync(u => u.AElfAddress == aElfAddress);
    }

    public async Task<AppIdentityUser> FindByCaHashAsync(string caHash)
    {
        return await _userRepository.FirstOrDefaultAsync(u => u.CaHash == caHash);
    }
}