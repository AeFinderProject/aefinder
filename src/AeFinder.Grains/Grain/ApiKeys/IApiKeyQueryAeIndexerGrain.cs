namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeyQueryAeIndexerGrain : IGrainWithStringKey
{
    Task RecordQueryCountAsync(Guid organizationId, Guid apiKeyId, string appId, long query,
        DateTime dateTime);
}