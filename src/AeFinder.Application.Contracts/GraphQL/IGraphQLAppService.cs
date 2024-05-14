using System.Net.Http;
using System.Threading.Tasks;
using AeFinder.GraphQL.Dto;

namespace AeFinder.GraphQL;

public interface IGraphQLAppService
{
    Task<HttpResponseMessage> RequestForwardAsync(string appId, string version, GraphQLQueryInput input);
    Task<string> GetAppCurrentVersionCacheNameAsync(string appId);
    Task CacheAppCurrentVersionAsync(string appId, string currentVersion);
    Task<string> GetAppCurrentVersionCacheAsync(string appId);
}