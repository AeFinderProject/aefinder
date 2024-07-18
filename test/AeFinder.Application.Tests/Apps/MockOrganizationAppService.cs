using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.User;
using AeFinder.User.Dto;
using Volo.Abp.Identity;

namespace AeFinder.Apps;

public class MockOrganizationAppService: AeFinderAppService, IOrganizationAppService
{
    public Task<OrganizationUnitDto> CreateOrganizationUnitAsync(string displayName, Guid? parentId = null)
    {
        throw new NotImplementedException();
    }

    public Task DeleteOrganizationUnitAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task AddUserToOrganizationUnitAsync(Guid userId, Guid organizationUnitId)
    {
        throw new NotImplementedException();
    }

    public Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync()
    {
        throw new NotImplementedException();
    }

    public Task<OrganizationUnitDto> GetOrganizationUnitAsync(Guid id)
    {
        throw new NotImplementedException();
    }

    public Task<List<IdentityUserDto>> GetUsersInOrganizationUnitAsync(Guid organizationUnitId)
    {
        throw new NotImplementedException();
    }
    
    public Task<List<OrganizationUnitDto>> GetOrganizationUnitsByUserIdAsync(Guid userId)
    {
        return Task.FromResult(new List<OrganizationUnitDto>
        {
            new OrganizationUnitDto
            {
                Id = Guid.Parse("99e439c3-49af-4caf-ad7e-417421eb98a1")
            }
        });
    }
}