using AeFinder.Grains.State.Users;
using AeFinder.User.Dto;
using Volo.Abp.ObjectMapping;

namespace AeFinder.Grains.Grain.Users;

public class UserRegisterGrain : AeFinderGrain<UserRegisterState>, IUserRegisterGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserRegisterGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task SetAsync(Guid userId, string organizationName)
    {
        await ReadStateAsync();
        State.UserId = userId;
        State.OrganizationName = organizationName;
        await WriteStateAsync();
    }

    public async Task<UserRegisterInfo> GetAsync()
    {
        await ReadStateAsync();
        return _objectMapper.Map<UserRegisterState, UserRegisterInfo>(State);
    }
}