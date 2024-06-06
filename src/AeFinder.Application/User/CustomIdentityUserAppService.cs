using System;
using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Auditing;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Identity;

namespace AeFinder.User;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
[Dependency(ReplaceServices = true)]
public class CustomIdentityUserAppService: IIdentityUserAppService, ITransientDependency
{

    public async Task<ListResultDto<IdentityRoleDto>> GetRolesAsync(Guid id)
    {
        return null;
    }

    public async Task<ListResultDto<IdentityRoleDto>> GetAssignableRolesAsync()
    {
        return null;
    }

    public async Task UpdateRolesAsync(Guid id, IdentityUserUpdateRolesDto input)
    {
        return;
    }

    public async Task<IdentityUserDto> FindByUsernameAsync(string userName)
    {
        return null;
    }

    public async Task<IdentityUserDto> FindByEmailAsync(string email)
    {
        return null;
    }

    public async Task<IdentityUserDto> CreateAsync(IdentityUserCreateDto input)
    {
        return null;
    }

    public async Task<IdentityUserDto> UpdateAsync(Guid id, IdentityUserUpdateDto input)
    {
        return null;
    }

    public async Task DeleteAsync(Guid id)
    {
        return;
    }

    public async Task<IdentityUserDto> GetAsync(Guid id)
    {
        return null;
    }

    public async Task<PagedResultDto<IdentityUserDto>> GetListAsync(GetIdentityUsersInput input)
    {
        return null;
    }
}