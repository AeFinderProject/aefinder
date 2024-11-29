using AeFinder.ApiKeys;

namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryBasicDataGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, BasicDataApiType basicDataApiType, long query,
        DateTime dateTime);
}