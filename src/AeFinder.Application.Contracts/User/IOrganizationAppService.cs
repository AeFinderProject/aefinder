using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Identity;

namespace AeFinder.User;

public interface IOrganizationAppService
{
    Task<OrganizationUnit> CreateOrganizationUnitAsync(string displayName, Guid? parentId = null);

    Task DeleteOrganizationUnitAsync(Guid id);

    Task AddUserToOrganizationUnitAsync(Guid userId, Guid organizationUnitId);

    Task<List<OrganizationUnit>> GetAllOrganizationUnitsAsync();

    Task<OrganizationUnit> GetOrganizationUnitAsync(Guid id);

    Task<List<IdentityUser>> GetUsersInOrganizationUnitAsync(Guid organizationUnitId);
}