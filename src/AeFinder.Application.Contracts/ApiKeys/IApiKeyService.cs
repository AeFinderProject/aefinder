using System;
using System.Threading.Tasks;

namespace AeFinder.ApiKeys;

public interface IApiKeyService
{
    Task IncreaseQueryAeIndexerCountAsync(string key, string appId);
    Task IncreaseQueryBasicDataCountAsync(string key, BasicDataApiType basicDataApiType);
}