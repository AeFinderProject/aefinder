using System;
using System.Threading.Tasks;
using AeFinder.Market;
using AeFinder.Options;
using AeFinder.User.Dto;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
        var organizationUnitExtension = await _organizationExtensionRepository.FindAsync(organizationGuid);
        if (organizationUnitExtension == null)
        {
            var indexerOrganizationInfoDto =
                await _indexerProvider.GetUserOrganizationInfoAsync(userWalletAddress,
                    _contractOptions.BillingContractChainId);
            if (indexerOrganizationInfoDto != null && indexerOrganizationInfoDto.Organization != null &&
                indexerOrganizationInfoDto.Organization.Items != null &&
                indexerOrganizationInfoDto.Organization.Items.Count > 0)
            {
                var organizationInfo = indexerOrganizationInfoDto.Organization.Items[0];
                await SaveOrganizationExtensionInfoAsync(new OrganizationExtensionDto()
                {
                    OrganizationId = organizationGuid,
                    OrganizationWalletAddress = organizationInfo.Address
                });
                return organizationInfo.Address;
            }

            return null;
        }

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
}