using System.Threading.Tasks;
using AeFinder.Common;
using GraphQL;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Market;

public interface IAeFinderIndexerProvider
{
    Task<IndexerUserFundRecordDto> GetUserFundRecordAsync(string address, string billingId);
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
                            id,chainId,address,transactionId,amount,token{symbol},balance,lockedBalance,type,billingId}
                }",
            Variables = new
            {
                address, billingId, skipCount = 0, maxResultCount = 10
            }
        });
    }
    
}