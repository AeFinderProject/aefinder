using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.App;
using AeFinder.App.Es;
using AeFinder.Commons;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Apps;
using AeFinder.GraphQL.Dto;
using AeFinder.Options;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using AElf.EntityMapping.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
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
    private readonly IdentityUserManager _identityUserManager;
    private readonly IIdentityUserRepository _identityUserRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IEntityMappingRepository<OrganizationIndex, string> _organizationEntityMappingRepository;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;

    public OrganizationAppService(IClusterClient clusterClient, 
        OrganizationUnitManager organizationUnitManager,
        IRepository<OrganizationUnit, Guid> organizationUnitRepository,
        IIdentityUserRepository identityUserRepository,
        IOrganizationInformationProvider organizationInformationProvider,
        IUserInformationProvider userInformationProvider,
        IAeFinderIndexerProvider indexerProvider, IOptionsSnapshot<ContractOptions> contractOptions,
        IEntityMappingRepository<OrganizationIndex, string> organizationEntityMappingRepository,
        IdentityUserManager identityUserManager)
    {
        _clusterClient = clusterClient;
        _organizationUnitManager = organizationUnitManager;
        _organizationUnitRepository = organizationUnitRepository;
        _identityUserManager = identityUserManager;
        _identityUserRepository = identityUserRepository;
        _organizationEntityMappingRepository = organizationEntityMappingRepository;
        _organizationInformationProvider = organizationInformationProvider;
        _userInformationProvider = userInformationProvider;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
    }
    
    public async Task<OrganizationUnitDto> CreateOrganizationUnitAsync(string displayName, Guid? parentId = null)
    {
        displayName = displayName.Trim();
        var organizationUnit = new OrganizationUnit(
            GuidGenerator.Create(), // Generate a unique identifier
            displayName,           // Organizational unit Displays name
            parentId,                   // the ID of the parent organizational unit, here is the root organizational unit
            CurrentTenant.Id
        );
        await _organizationUnitManager.CreateAsync(organizationUnit);
        await CurrentUnitOfWork.SaveChangesAsync();
        var organizationUnitDto = ObjectMapper.Map<OrganizationUnit, OrganizationUnitDto>(organizationUnit);
        
        //Synchronize organization info into grain & es
        var organizationAppGain =
            _clusterClient.GetGrain<IOrganizationAppGrain>(
                GrainIdHelper.GenerateOrganizationAppGrainId(organizationUnitDto.Id.ToString("N")));
        await organizationAppGain.AddOrganizationAsync(organizationUnit.DisplayName);
        
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

        await CurrentUnitOfWork.SaveChangesAsync();
    }

    public async Task<List<OrganizationUnitDto>> GetAllOrganizationUnitsAsync()
    {
        var organizationUnits = await _organizationUnitRepository.GetListAsync();
        var result = ObjectMapper.Map<List<OrganizationUnit>, List<OrganizationUnitDto>>(organizationUnits);
        return result;
    }

    public async Task<PagedResultDto<OrganizationIndexDto>> GetOrganizationListAsync(GetOrganizationListInput input)
    {
        var queryable = await _organizationEntityMappingRepository.GetQueryableAsync();
        var organizations = queryable.OrderBy(o => o.OrganizationName).Skip(input.SkipCount).Take(input.MaxResultCount).ToList();
        var totalCount = queryable.Count();
        return new PagedResultDto<OrganizationIndexDto>
        {
            TotalCount = totalCount,
            Items = ObjectMapper.Map<List<OrganizationIndex>, List<OrganizationIndexDto>>(organizations)
        };
    }
    
    public async Task<OrganizationUnitDto> GetOrganizationUnitAsync(Guid id)
    {
        var organizationUnit = await _organizationUnitRepository.GetAsync(id);
        var organizationUnitDto = ObjectMapper.Map<OrganizationUnit, OrganizationUnitDto>(organizationUnit);
        var organizationExtensionInfo =
            await _organizationInformationProvider.GetOrganizationUnitExtensionInfoAsync(organizationUnitDto.Id);
        organizationUnitDto.OrganizationStatus = organizationExtensionInfo.OrganizationStatus;
        organizationUnitDto.OrganizationWalletAddress = organizationExtensionInfo.OrganizationWalletAddress;
        return organizationUnitDto;
    }

    public async Task<List<IdentityUserDto>> GetUsersInOrganizationUnitAsync(Guid organizationUnitId)
    {
        var users = await _identityUserRepository.GetUsersInOrganizationUnitAsync(organizationUnitId);

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
        var organizationUnits = await _identityUserRepository.GetOrganizationUnitsAsync(userId);

        var result = ObjectMapper.Map<List<OrganizationUnit>, List<OrganizationUnitDto>>(organizationUnits);
        foreach (var item in result)
        {
            var organizationExtensionInfo =
                await _organizationInformationProvider.GetOrganizationUnitExtensionInfoAsync(item.Id);
            item.OrganizationStatus = organizationExtensionInfo.OrganizationStatus;
            item.OrganizationWalletAddress = organizationExtensionInfo.OrganizationWalletAddress;
        }
        return result;
    }
    
    public async Task<OrganizationBalanceDto> GetOrganizationBalanceAsync()
    {
        var organizationUnit = await GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var userExtensionInfo = await _userInformationProvider.GetUserExtensionInfoByIdAsync(CurrentUser.Id.Value);
        if (userExtensionInfo.WalletAddress.IsNullOrEmpty())
        {
            Logger.LogError(
                $"user:{CurrentUser.Id.Value} does not bind any wallet");
            throw new UserFriendlyException("Please bind your user wallet first.");
        }

        var organizationWalletAddress = await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(
            organizationId, userExtensionInfo.WalletAddress);
        if (string.IsNullOrEmpty(organizationWalletAddress))
        {
            return new OrganizationBalanceDto();
            // throw new UserFriendlyException($"The user has not linked any organization wallet address yet.");
        }
        Logger.LogInformation(
            $"userWalletAddress:{userExtensionInfo.WalletAddress} organizationWalletAddress:{organizationWalletAddress}");
        var indexerBalanceDto =
            await _indexerProvider.GetUserBalanceAsync(organizationWalletAddress,
                _contractOptions.BillingContractChainId, 0, 10);
        if (indexerBalanceDto == null || indexerBalanceDto.UserBalance == null ||
            indexerBalanceDto.UserBalance.Items == null || indexerBalanceDto.UserBalance.Items.Count == 0)
        {
            return new OrganizationBalanceDto();
        }

        var balanceInfo = indexerBalanceDto.UserBalance.Items[0];
        var organizationAccountBalance = ObjectMapper.Map<UserBalanceDto,OrganizationBalanceDto>(balanceInfo);
        organizationAccountBalance.ChainId = balanceInfo.Metadata.ChainId;
        return organizationAccountBalance;
    }
    
    public async Task<OrganizationUnit> GetUserDefaultOrganizationAsync(Guid userId)
    {
        var organizationUnits = await _identityUserRepository.GetOrganizationUnitsAsync(userId);
        if (organizationUnits == null)
        {
            return null;
        }

        return organizationUnits.FirstOrDefault();
    }

    public async Task FreezeOrganizationAsync(Guid organizationUnitId)
    {
        await _organizationInformationProvider.FreezeOrganizationAsync(organizationUnitId);
    }

    public async Task UnFreezeOrganizationAsync(Guid organizationUnitId)
    {
        await _organizationInformationProvider.UnFreezeOrganizationAsync(organizationUnitId);
    }
}