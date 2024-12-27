using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User.Dto;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Identity;

namespace AeFinder.User;

public interface IOrganizationAppService
{
    Task<OrganizationUnitDto> CreateOrganizationUnitAsync(string displayName, Guid? parentId = null);

    Task DeleteOrganizationUnitAsync(Guid id);

    Task AddUserToOrganizationUnitAsync(Guid userId, Guid organizationUnitId);

    Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync();
    Task<PagedResultDto<OrganizationIndexDto>> GetOrganizationListAsync(GetOrganizationListInput input);
    Task<OrganizationUnitDto> GetOrganizationUnitAsync(Guid id);

    Task<List<IdentityUserDto>> GetUsersInOrganizationUnitAsync(Guid organizationUnitId);

    Task<List<OrganizationUnitDto>> GetOrganizationUnitsByUserIdAsync(Guid userId);
    Task<OrganizationUnit> GetUserDefaultOrganizationAsync(Guid userId);
    Task<OrganizationBalanceDto> GetOrganizationBalanceAsync();
}