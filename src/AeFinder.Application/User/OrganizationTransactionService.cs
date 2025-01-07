using System.Collections.Generic;
using System.Threading.Tasks;
using AeFinder.Commons;
using AeFinder.Enums;
using AeFinder.Options;
using AeFinder.User.Dto;
using AeFinder.User.Provider;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace AeFinder.User;

public class OrganizationTransactionService: AeFinderAppService, IOrganizationTransactionService
{
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly ContractOptions _contractOptions;
    
    public OrganizationTransactionService(IOrganizationAppService organizationAppService,
        IAeFinderIndexerProvider indexerProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOrganizationInformationProvider organizationInformationProvider)
    {
        _organizationAppService = organizationAppService;
        _indexerProvider = indexerProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _contractOptions = contractOptions.Value;
    }
    
    public async Task<PagedResultDto<TransactionHistoryDto>> GetOrganizationTransactionHistoryAsync(GetTransactionHistoryInput input)
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var organizationWalletAddress =
            await _organizationInformationProvider.GetOrganizationWalletAddressAsync(organizationId);
        if (string.IsNullOrEmpty(organizationWalletAddress))
        {
            return new PagedResultDto<TransactionHistoryDto>();
        }

        var indexerUserFundRecordDto =
            await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId,
                organizationWalletAddress, null, input.SkipCount, input.MaxResultCount);
        if (indexerUserFundRecordDto == null || indexerUserFundRecordDto.UserFundRecord == null)
        {
            return new PagedResultDto<TransactionHistoryDto>();
        }

        var result = new List<TransactionHistoryDto>();
        foreach (var userFundRecordDto in indexerUserFundRecordDto.UserFundRecord.Items)
        {
            var dto = new TransactionHistoryDto();
            dto.TransactionId = userFundRecordDto.TransactionId;
            switch (userFundRecordDto.Type)
            {
                case UserFundRecordType.Deposit:
                {
                    dto.TransactionDescription = "Balance Deposit";
                    break;
                }
                case UserFundRecordType.Charge:
                {
                    dto.TransactionDescription = "Charge Locked Balance";
                    break;
                }
                case UserFundRecordType.Lock:
                {
                    dto.TransactionDescription = "Lock Balance";
                    break;
                }
                case UserFundRecordType.Unlock:
                {
                    dto.TransactionDescription = "Balance Unlock";
                    break;
                }
                case UserFundRecordType.Withdrawal:
                {
                    dto.TransactionDescription = "Balance Withdrawal";
                    break;
                }
            }

            dto.TransactionDate = userFundRecordDto.Metadata.Block.BlockTime;
            dto.TransactionAmount = userFundRecordDto.Amount;
            dto.BalanceAfter = userFundRecordDto.Balance;
            dto.LockedBalance = userFundRecordDto.LockedBalance;
            dto.PaymentMethod = userFundRecordDto.Token.Symbol;
            result.Add(dto);
        }

        return new PagedResultDto<TransactionHistoryDto>()
        {
            TotalCount = indexerUserFundRecordDto.UserFundRecord.TotalCount,
            Items = result
        };
    }
}