using System.Threading.Tasks;
using AeFinder.Common;
using AeFinder.Commons;
using GraphQL;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Market;

public interface IAeFinderIndexerProvider
{
    Task<IndexerUserFundRecordDto> GetUserFundRecordAsync(string address, string billingId);
    Task<IndexerUserBalanceDto> GetUserBalanceAsync(string address, string chainId);
    Task<IndexerOrganizationInfoDto> GetUserOrganizationInfoAsync(string memberAddress, string chainId);
}

public class AeFinderIndexerProvider: IAeFinderIndexerProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    
    public AeFinderIndexerProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<IndexerUserFundRecordDto> GetUserFundRecordAsync(string address, string billingId)
    {
        var result= await _graphQlHelper.QueryAsync<IndexerUserFundRecordDto>(new GraphQLRequest
        {
            Query = @"
			    query($address:String,$billingId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    userFundRecord(input: {address:$address,billingId:$billingId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,metadata{chainId,block{blockHash,blockHeight,blockTime}},address,transactionId,amount,token{symbol},balance,lockedBalance,type,billingId},totalCount}
                }",
            Variables = new
            {
                address, billingId, skipCount = 0, maxResultCount = 10
            }
        });
        if (result != null && result.UserFundRecord != null && result.UserFundRecord.Items != null &&
            result.UserFundRecord.Items.Count > 0)
        {
            foreach (var userFundRecordDto in result.UserFundRecord.Items)
            {
                userFundRecordDto.LockedBalance = userFundRecordDto.LockedBalance / ContractConstant.USDTDecimals;
                userFundRecordDto.Balance = userFundRecordDto.Balance / ContractConstant.USDTDecimals;
                userFundRecordDto.Amount = userFundRecordDto.Amount / ContractConstant.USDTDecimals;
            }

            return result;
        }

        return result;
    }

    public async Task<IndexerUserBalanceDto> GetUserBalanceAsync(string address, string chainId)
    {
        var result= await _graphQlHelper.QueryAsync<IndexerUserBalanceDto>(new GraphQLRequest
        {
            Query = @"
			    query($address:String,$chainId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    userBalance(input: {address:$address,chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,metadata{chainId,block{blockHash,blockHeight,blockTime}},address,symbol,balance,lockedBalance,token{symbol}},totalCount}
                }",
            Variables = new
            {
                address, chainId, skipCount = 0, maxResultCount = 10
            }
        });
        if (result != null && result.UserBalance != null && result.UserBalance.Items != null &&
            result.UserBalance.Items.Count > 0)
        {
            foreach (var userBalanceDto in result.UserBalance.Items)
            {
                userBalanceDto.Balance = userBalanceDto.Balance / ContractConstant.USDTDecimals;
                userBalanceDto.LockedBalance = userBalanceDto.LockedBalance / ContractConstant.USDTDecimals;
            }

            return result;
        }

        return result;
    }

    public async Task<IndexerOrganizationInfoDto> GetUserOrganizationInfoAsync(string memberAddress, string chainId)
    {
        return await _graphQlHelper.QueryAsync<IndexerOrganizationInfoDto>(new GraphQLRequest
        {
            Query = @"
			    query($memberAddress:String,$chainId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    organization(input: {memberAddress:$memberAddress,chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,metadata{chainId,block{blockHash,blockHeight,blockTime}},address,members{address,role}},totalCount}
                }",
            Variables = new
            {
                memberAddress, chainId, skipCount = 0, maxResultCount = 10
            }
        });
    }

}