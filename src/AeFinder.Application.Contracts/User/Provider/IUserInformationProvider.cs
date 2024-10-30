using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Options;
using AeFinder.User.Dto;

namespace AeFinder.User.Provider;

public interface IUserInformationProvider
{
    Task<bool> SaveUserExtensionInfoAsync(UserExtensionDto userExtensionDto);

    Task<UserExtensionDto> GetUserExtensionInfoByIdAsync(Guid userId);
}