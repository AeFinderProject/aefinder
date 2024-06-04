using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App;
using AeFinder.User.Dto;
using Volo.Abp;
using Volo.Abp.Auditing;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.Identity;
using IdentityUser = Volo.Abp.Identity.IdentityUser;

namespace AeFinder.User;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class OrganizationAppService: AeFinderAppService, IOrganizationAppService
{
    private readonly OrganizationUnitManager _organizationUnitManager;
    private readonly IRepository<OrganizationUnit, Guid> _organizationUnitRepository;
    // private readonly UserManager<IdentityUser> _userManager;
    private readonly IdentityUserManager _identityUserManager;
    private readonly IRepository<IdentityUser, Guid> _identityUserRepository;
    private readonly IRepository<ExtendedIdentityUserOrganizationUnit> _identityUserOrganizationUnitRepository;

    public OrganizationAppService(
        OrganizationUnitManager organizationUnitManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository,
        IRepository<IdentityUser, Guid> identityUserRepository,
        IRepository<ExtendedIdentityUserOrganizationUnit> identityUserOrganizationUnitRepository,
        IdentityUserManager identityUserManager)
    {
        _organizationUnitManager = organizationUnitManager;
        _organizationUnitRepository = organizationUnitRepository;
        // _userManager = userManager;
        _identityUserManager = identityUserManager;
        _identityUserRepository = identityUserRepository;
        _identityUserOrganizationUnitRepository = identityUserOrganizationUnitRepository;
    }
    
    public async Task<OrganizationUnitDto> CreateOrganizationUnitAsync(string displayName, Guid? parentId = null)
    {
        var organizationUnit = new OrganizationUnit(
            GuidGenerator.Create(), // Generate a unique identifier
            displayName,           // Organizational unit Displays name
            parentId,                   // the ID of the parent organizational unit, here is the root organizational unit
            CurrentTenant.Id
        );
        await _organizationUnitManager.CreateAsync(organizationUnit);
        await CurrentUnitOfWork.SaveChangesAsync();
        var organizationUnitDto = ObjectMapper.Map<OrganizationUnit, OrganizationUnitDto>(organizationUnit);
        return organizationUnitDto;
    }


    public async Task DeleteOrganizationUnitAsync(Guid id)
    {
        var organizationUnit = await _organizationUnitRepository.GetAsync(id);
        await _organizationUnitManager.DeleteAsync(id);
    }
    
    public async Task AddUserToOrganizationUnitAsync(Guid userId, Guid organizationUnitId)
    {
        var user = await _identityUserManager.GetByIdAsync(userId);
        if (user == null) {
            throw new UserFriendlyException("User not found.");
        }

        var organizationUnit = await _organizationUnitRepository.GetAsync(organizationUnitId);
        if (organizationUnit == null) {
            throw new UserFriendlyException("Organization unit not found.");
        }
        
        await _identityUserManager.AddToOrganizationUnitAsync(user.Id, organizationUnit.Id);

        var userOU = new ExtendedIdentityUserOrganizationUnit(userId, organizationUnitId, CurrentTenant.Id);
        await _identityUserOrganizationUnitRepository.InsertAsync(userOU);
        
        await CurrentUnitOfWork.SaveChangesAsync();
    }

    public async Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync()
    {
        var organizationUnits = await _organizationUnitRepository.GetListAsync();
        var result = ObjectMapper.Map<List<OrganizationUnit>, List<OrganizationUnitDto>>(organizationUnits);
        return result;
    }
    
    public async Task<OrganizationUnitDto> GetOrganizationUnitAsync(Guid id)
    {
        var organizationUnit = await _organizationUnitRepository.GetAsync(id);
        var organizationUnitDto = ObjectMapper.Map<OrganizationUnit, OrganizationUnitDto>(organizationUnit);
            
        return organizationUnitDto;
    }

    public async Task<List<IdentityUserDto>> GetUsersInOrganizationUnitAsync(Guid organizationUnitId)
    {
        // the associated query of organizational units and users
        var userOrgUnitsQuery = await _identityUserOrganizationUnitRepository.GetQueryableAsync();
        // var userOrgUnits = await AsyncExecuter.ToListAsync(
        //     userOrgUnitsQuery.Where(uou => uou.OrganizationUnitId == organizationUnitId));
        var userOrgUnits = userOrgUnitsQuery.Where(uou => uou.OrganizationUnitId == organizationUnitId).ToList();
        
        var userIds = userOrgUnits.Select(uou => uou.UserId).ToList();

        // get users entity query
        var usersQuery = await _identityUserRepository.GetQueryableAsync();
        var users = await AsyncExecuter.ToListAsync(
            usersQuery.Where(user => userIds.Contains(user.Id)));

        return ObjectMapper.Map<List<IdentityUser>, List<IdentityUserDto>>(users);
    }
    
    public async Task<bool> IsUserAdminAsync(string userId)
    {
        var user = await _identityUserManager.FindByIdAsync(userId);
        if (user != null)
        {
            return await _identityUserManager.IsInRoleAsync(user, "Admin");
        }
        return false;
    }
    
    public async Task<List<OrganizationUnitDto>> GetOrganizationUnitsByUserIdAsync(Guid userId)
    {
        var organizationUnitList = await _identityUserOrganizationUnitRepository
            .GetListAsync(uou => uou.UserId == userId);

        if (organizationUnitList == null || organizationUnitList.Count == 0)
        {
            throw new UserFriendlyException("The user does not belong to any organization.");
        }

        var organizationUnitIds = organizationUnitList.Select(uou => uou.OrganizationUnitId)
            .ToList();

        var organizationUnitQuery = await _organizationUnitRepository.GetQueryableAsync();
        var organizationUnits = organizationUnitQuery
            .Where(ou => organizationUnitIds.Contains(ou.Id))
            .ToList();

        var result = ObjectMapper.Map<List<OrganizationUnit>, List<OrganizationUnitDto>>(organizationUnits);
        return result;
    }
}