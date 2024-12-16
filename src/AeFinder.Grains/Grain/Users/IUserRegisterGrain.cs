using AeFinder.User.Dto;

namespace AeFinder.Grains.Grain.Users;

public interface IUserRegisterGrain : IGrainWithStringKey
{
    Task SetAsync(Guid userId, string organizationName);
    Task<UserRegisterInfo> GetAsync();
}