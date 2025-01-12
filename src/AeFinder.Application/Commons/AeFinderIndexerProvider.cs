using System.Net.Http;
using System.Threading.Tasks;
using AeFinder.GraphQL.Dto;
using AeFinder.Options;
using GraphQL;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace AeFinder.Commons;

public interface IAeFinderIndexerProvider
{
    Task<long> GetCurrentVersionSyncBlockHeightAsync();
    Task<IndexerUserFundRecordDto> GetUserFundRecordAsync(string chainId, string address, string billingId,
        int skipCount, int maxResultCount);
    Task<IndexerUserBalanceDto> GetUserBalanceAsync(string address, string chainId, int skipCount,
        int maxResultCount);
    Task<IndexerOrganizationInfoDto> GetUserOrganizationInfoAsync(string memberAddress, string chainId,
        int skipCount, int maxResultCount);
}

public class AeFinderIndexerProvider: IAeFinderIndexerProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GraphQLOptions _graphQlOptions;

    public AeFinderIndexerProvider(IGraphQLHelper graphQlHelper, IHttpClientFactory httpClientFactory,
        IOptionsSnapshot<GraphQLOptions> graphQlOptions)
    {
        _graphQlHelper = graphQlHelper;
        _httpClientFactory = httpClientFactory;
        _graphQlOptions = graphQlOptions.Value;
    }
    
    public async Task<long> GetCurrentVersionSyncBlockHeightAsync()
    {
        string result = await QueryIndexerSyncStateAsync(_graphQlOptions.BillingIndexerSyncStateUrl);
        var resultDto = JsonConvert.DeserializeObject<IndexerSyncStateDto>(result);
        if (resultDto == null || resultDto.CurrentVersion == null || resultDto.CurrentVersion.Items == null ||
            resultDto.CurrentVersion.Items.Count == 0)
        {
            return 0;
        }
        return resultDto.CurrentVersion.Items[0].LongestChainHeight;
    }
    
    private async Task<string> QueryIndexerSyncStateAsync(string indexerSyncStateUrl)
    {
        var httpClient = _httpClientFactory.CreateClient();
        var uri = indexerSyncStateUrl;

        var response = await httpClient.GetAsync(uri);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    }
    
    public async Task<IndexerUserFundRecordDto> GetUserFundRecordAsync(string chainId, string address, string billingId,
        int skipCount, int maxResultCount)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerUserFundRecordDto>(new GraphQLRequest
        {
            Query = @"
			    query($address:String,$chainId:String,$billingId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    userFundRecord(input: {address:$address,chainId:$chainId,billingId:$billingId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,metadata{chainId,block{blockHash,blockHeight,blockTime}},address,transactionId,amount,token{symbol},balance,lockedBalance,type,billingId},totalCount}
                }",
            Variables = new
            {
                address, chainId, billingId, skipCount = skipCount, maxResultCount = maxResultCount
            }
        });

        return result;
    }
    
    public async Task<IndexerUserBalanceDto> GetUserBalanceAsync(string address, string chainId, int skipCount,
        int maxResultCount)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerUserBalanceDto>(new GraphQLRequest
        {
            Query = @"
			    query($address:String,$chainId:String,$skipCount:Int!,$maxResultCount:Int!) {
                    userBalance(input: {address:$address,chainId:$chainId,skipCount:$skipCount,maxResultCount:$maxResultCount}){
                            items{id,metadata{chainId,block{blockHash,blockHeight,blockTime}},address,symbol,balance,lockedBalance,token{symbol}},totalCount}
                }",
            Variables = new
            {
                address, chainId, skipCount = skipCount, maxResultCount = maxResultCount
            }
        });

        return result;
    }
    
    public async Task<IndexerOrganizationInfoDto> GetUserOrganizationInfoAsync(string memberAddress, string chainId,
        int skipCount, int maxResultCount)
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
                memberAddress, chainId, skipCount = skipCount, maxResultCount = maxResultCount
            }
        });
    }
}