using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AeFinder.Grains;
using AeFinder.Grains.Grain.Market;
using AeFinder.Options;
using AeFinder.User;
using AeFinder.User.Provider;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.Auditing;

namespace AeFinder.Market;

[RemoteService(IsEnabled = false)]
[DisableAuditing]
public class BillService : ApplicationService, IBillService
{
    private readonly IClusterClient _clusterClient;
    private readonly IOrganizationAppService _organizationAppService;
    private readonly IAeFinderIndexerProvider _indexerProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IOrganizationInformationProvider _organizationInformationProvider;
    private readonly ContractOptions _contractOptions;

    public BillService(IClusterClient clusterClient, IOrganizationAppService organizationAppService,
        IAeFinderIndexerProvider indexerProvider,IUserInformationProvider userInformationProvider,
        IOptionsSnapshot<ContractOptions> contractOptions,
        IOrganizationInformationProvider organizationInformationProvider)
    {
        _clusterClient = clusterClient;
        _organizationAppService = organizationAppService;
        _indexerProvider = indexerProvider;
        _userInformationProvider = userInformationProvider;
        _organizationInformationProvider = organizationInformationProvider;
        _contractOptions = contractOptions.Value;
    }

    public async Task<BillingPlanDto> GetProductBillingPlanAsync(GetBillingPlanInput input)
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();
        
        var result = new BillingPlanDto();
        var productsGrain =
            _clusterClient.GetGrain<IProductsGrain>(
                GrainIdHelper.GenerateProductsGrainId());
        var productInfo = await productsGrain.GetProductInfoByIdAsync(input.ProductId);
        result.MonthlyUnitPrice = productInfo.MonthlyUnitPrice;
        var monthlyFee = result.MonthlyUnitPrice * input.ProductNum;
        result.BillingCycleMonthCount = input.PeriodMonths;
        result.PeriodicCost = result.BillingCycleMonthCount * monthlyFee;
        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        if (productInfo.ProductType == ProductType.ApiQueryCount)
        {
            result.FirstMonthCost = monthlyFee;
            return result;
        }
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        result.FirstMonthCost = await billsGrain.CalculateFirstMonthLockAmount(monthlyFee);
        return result;
    }

    public async Task<PagedResultDto<TransactionHistoryDto>> GetOrganizationTransactionHistoryAsync()
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();

        var userExtensionDto =
            await _userInformationProvider.GetUserExtensionInfoByIdAsync(CurrentUser.Id.Value);
        if (userExtensionDto.WalletAddress.IsNullOrEmpty())
        {
            return new PagedResultDto<TransactionHistoryDto>();
        }
        var organizationWalletAddress =
            await _organizationInformationProvider.GetUserOrganizationWalletAddressAsync(organizationId,userExtensionDto.WalletAddress);
        if (organizationWalletAddress.IsNullOrEmpty())
        {
            return new PagedResultDto<TransactionHistoryDto>();
        }

        var indexerUserFundRecordDto =
            await _indexerProvider.GetUserFundRecordAsync(_contractOptions.BillingContractChainId,
                organizationWalletAddress, null);
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

    public async Task<PagedResultDto<InvoiceInfoDto>> GetInvoicesAsync()
    {
        //Check organization id
        var organizationUnit = await _organizationAppService.GetUserDefaultOrganizationAsync(CurrentUser.Id.Value);
        if (organizationUnit == null)
        {
            throw new UserFriendlyException("User has not yet bind any organization");
        }

        var organizationId = organizationUnit.Id.ToString();

        var organizationGrainId = await GetOrganizationGrainIdAsync(organizationId);
        var billsGrain =
            _clusterClient.GetGrain<IBillsGrain>(organizationGrainId);
        var bills=await billsGrain.GetOrganizationAllBillsAsync(organizationId);
        var invoiceInfoList = new List<InvoiceInfoDto>();
        foreach (var billDto in bills)
        {
            var invoiceInfo = ObjectMapper.Map<BillDto, InvoiceInfoDto>(billDto);
            invoiceInfoList.Add(invoiceInfo);
            if (billDto.RefundAmount > 0)
            {
                var refundInvoiceInfo = ObjectMapper.Map<BillDto, InvoiceInfoDto>(billDto);
                refundInvoiceInfo.BillingAmount = billDto.RefundAmount;
                refundInvoiceInfo.BillingType = BillingType.Refund;
                invoiceInfoList.Add(refundInvoiceInfo);
            }
        }
        return new PagedResultDto<InvoiceInfoDto>
        {
            TotalCount = invoiceInfoList.Count,
            Items = invoiceInfoList
        };
    }

    private async Task<string> GetOrganizationGrainIdAsync()
    {
        var organizationIds = await _organizationAppService.GetOrganizationUnitsByUserIdAsync(CurrentUser.Id.Value);
        return organizationIds.First().Id.ToString("N");
    }
    
    private async Task<string> GetOrganizationGrainIdAsync(string organizationId)
    {
        var organizationGuid = Guid.Parse(organizationId);
        return organizationGuid.ToString("N");
    }
}