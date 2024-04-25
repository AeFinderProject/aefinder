using AeFinder.Grains.State.Apps;
using Orleans;

namespace AeFinder.Grains.Grain.Apps;

public interface IUserManageGrain : IGrainWithStringKey
{
    Task<bool> ExistsAdmin();

    Task<bool> SetAdmin(string userId);

    Task<bool> IsAdmin(string userId);
}

public class UserManageGrain : Grain<UserManageState>, IUserManageGrain
{
    public Task<bool> ExistsAdmin()
    {
        return Task.FromResult(State.AdminUserId != null);
    }

    public async Task<bool> SetAdmin(string userId)
    {
        if (State.AdminUserId != null)
        {
            return false;
        }

        State.AdminUserId = userId;
        await WriteStateAsync();
        return true;
    }

    public Task<bool> IsAdmin(string userId)
    {
        return Task.FromResult(State.AdminUserId != null && State.AdminUserId == userId);
    }
}