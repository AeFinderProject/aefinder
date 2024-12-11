using System.Threading.Tasks;
using AeFinder.Common;
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
        return await _graphQlHelper.QueryAsync<IndexerUserFundRecordDto>(new GraphQLRequest
        {
            Query = @"
			    query($address:String,$billingId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    userFundRecord(input: {address:$address,billingId:$billingId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,chainId,address,transactionId,amount,token{symbol},balance,lockedBalance,type,billingId},totalCount}
                }",
            Variables = new
            {
                address, billingId, skipCount = 0, maxResultCount = 10
            }
        });
    }

    public async Task<IndexerUserBalanceDto> GetUserBalanceAsync(string address, string chainId)
    {
        return await _graphQlHelper.QueryAsync<IndexerUserBalanceDto>(new GraphQLRequest
        {
            Query = @"
			    query($address:String,$chainId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    userBalance(input: {address:$address,chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,chainId,address,symbol,balance,lockedBalance,token{symbol}},totalCount}
                }",
            Variables = new
            {
                address, chainId, skipCount = 0, maxResultCount = 10
            }
        });
    }

    public async Task<IndexerOrganizationInfoDto> GetUserOrganizationInfoAsync(string memberAddress, string chainId)
    {
        return await _graphQlHelper.QueryAsync<IndexerOrganizationInfoDto>(new GraphQLRequest
        {
            Query = @"
			    query($memberAddress:String,$chainId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    organization(input: {memberAddress:$memberAddress,chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,chainId,address,members{address,role}},totalCount}
                }",
            Variables = new
            {
                memberAddress, chainId, skipCount = 0, maxResultCount = 10
            }
        });
    }

}