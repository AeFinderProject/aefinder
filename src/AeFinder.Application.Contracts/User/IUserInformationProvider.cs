using System;
using System.Threading.Tasks;
using AeFinder.User.Dto;

namespace AeFinder.User;

public interface IUserInformationProvider
{
    Task<bool> SaveUserExtensionInfoAsync(UserExtensionDto userExtensionDto);

    Task<UserExtensionDto> GetUserExtensionInfoAsync(Guid userId);
}