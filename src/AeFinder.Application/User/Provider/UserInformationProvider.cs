using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.User.Dto;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace AeFinder.User.Provider;

public class UserInformationProvider: IUserInformationProvider, ISingletonDependency
{
    private readonly ILogger<UserInformationProvider> _logger;
    private readonly IRepository<IdentityUserExtension, Guid> _userExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    

    public UserInformationProvider(IRepository<IdentityUserExtension, Guid> userExtensionRepository,
        IObjectMapper objectMapper,ILogger<UserInformationProvider> logger)
    {
        _userExtensionRepository = userExtensionRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<bool> SaveUserExtensionInfoAsync(UserExtensionDto userExtensionDto)
    {
        var userExtension = await _userExtensionRepository.FirstOrDefaultAsync(x => x.Id == userExtensionDto.UserId);
        if (userExtension == null)
        {
            // var caAddressMain=userExtensionDto.CaAddressList
            userExtension = new IdentityUserExtension(userExtensionDto.UserId)
            {
                UserId = userExtensionDto.UserId,
                AElfAddress = userExtensionDto.AElfAddress,
                CaHash = userExtensionDto.CaHash
            };
            if (userExtensionDto.CaAddressList != null && userExtensionDto.CaAddressList.Count > 0)
            {
                userExtension.CaAddressList =
                    _objectMapper.Map<List<UserChainAddressDto>, List<UserChainAddressInfo>>(userExtensionDto
                        .CaAddressList);
                var caAddressMain = userExtension.CaAddressList.FirstOrDefault(u => u.ChainId.ToUpper() == "AELF");
                userExtension.CaAddressMain = caAddressMain == null ? string.Empty : caAddressMain.Address;
            }

            await _userExtensionRepository.InsertAsync(userExtension);
            return true;
        }

        return false;
    }

    public async Task<UserExtensionDto> GetUserExtensionInfoByIdAsync(Guid userId)
    {
        var userExtension = await _userExtensionRepository.FindAsync(userId);
        if (userExtension == null)
        {
            return new UserExtensionDto();
        }
        return _objectMapper.Map<IdentityUserExtension,UserExtensionDto>(userExtension);
    }
}