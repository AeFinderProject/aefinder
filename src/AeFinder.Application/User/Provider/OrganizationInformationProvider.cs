using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Commons;
using AeFinder.Options;
using AeFinder.User.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;
using Volo.Abp.Domain.Repositories;
using Volo.Abp.ObjectMapping;

namespace AeFinder.User.Provider;

public class OrganizationInformationProvider: IOrganizationInformationProvider, ISingletonDependency
{
    private readonly ILogger<OrganizationInformationProvider> _logger;
    private readonly IRepository<OrganizationUnitExtension, Guid> _organizationExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly ContractOptions _contractOptions;

    public OrganizationInformationProvider(IRepository<OrganizationUnitExtension, Guid> organizationExtensionRepository,
        IAeFinderIndexerProvider indexerProvider, IOptionsSnapshot<ContractOptions> contractOptions,
        IObjectMapper objectMapper, ILogger<OrganizationInformationProvider> logger)
    {
        _organizationExtensionRepository = organizationExtensionRepository;
        _objectMapper = objectMapper;
        _logger = logger;
        _indexerProvider = indexerProvider;
        _contractOptions = contractOptions.Value;
    }
    
    public async Task<string> GetUserOrganizationWalletAddressAsync(string organizationId, string userWalletAddress)
    {
        var organizationGuid = Guid.Parse(organizationId);
        // _logger.LogInformation($"[GetUserOrganizationWalletAddressAsync]find organizationId:{organizationGuid.ToString()}");
        var organizationUnitExtension = await _organizationExtensionRepository.FindAsync(organizationGuid);
        if (organizationUnitExtension == null)
        {
            if (string.IsNullOrEmpty(userWalletAddress))
            {
                throw new Exception("user wallet address can not be null");
            }

            var indexerOrganizationInfoDto =
                await _indexerProvider.GetUserOrganizationInfoAsync(userWalletAddress,
                    _contractOptions.BillingContractChainId, 0, 10);
            if (indexerOrganizationInfoDto != null && indexerOrganizationInfoDto.Organization != null &&
                indexerOrganizationInfoDto.Organization.Items != null &&
                indexerOrganizationInfoDto.Organization.Items.Count > 0)
            {
                var organizationInfo = indexerOrganizationInfoDto.Organization.Items[0];
                _logger.LogInformation($"[GetUserOrganizationWalletAddressAsync]indexer organization:{JsonConvert.SerializeObject(organizationInfo)}");
                await SaveOrganizationExtensionInfoAsync(new OrganizationExtensionDto()
                {
                    OrganizationId = organizationGuid,
                    OrganizationWalletAddress = organizationInfo.Address
                });
                return organizationInfo.Address;
            }

            return null;
        }
        _logger.LogInformation($"[GetUserOrganizationWalletAddressAsync]organization:{JsonConvert.SerializeObject(organizationUnitExtension)}");
        return organizationUnitExtension.OrganizationWalletAddress;
    }

    public async Task<bool> SaveOrganizationExtensionInfoAsync(OrganizationExtensionDto organizationExtensionDto)
    {
        var organizationUnitExtension = await _organizationExtensionRepository.FirstOrDefaultAsync(x => x.Id == organizationExtensionDto.OrganizationId);
        if (organizationUnitExtension == null)
        {
            organizationUnitExtension = new OrganizationUnitExtension(organizationExtensionDto.OrganizationId)
            {
                OrganizationId = organizationExtensionDto.OrganizationId,
                OrganizationWalletAddress = organizationExtensionDto.OrganizationWalletAddress
            };

            await _organizationExtensionRepository.InsertAsync(organizationUnitExtension);
            return true;
        }

        return false;
    }

    public async Task<string> GetOrganizationWalletAddressAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        var organizationUnitExtension = await _organizationExtensionRepository.FindAsync(organizationGuid);
        if (organizationUnitExtension == null)
        {
            return null;
        }
        return organizationUnitExtension.OrganizationWalletAddress;
    }

    public async Task<List<Guid>> GetOrganizationWithoutWalletListAsync()
    {
        var organizationList =
            await _organizationExtensionRepository.GetListAsync(o => string.IsNullOrEmpty(o.OrganizationWalletAddress));
        return organizationList.Select(o=>o.OrganizationId).ToList();
    }
}