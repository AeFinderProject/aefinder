namespace AeFinder.Grains.Grain.ApiKeys;

public interface IApiKeySummaryGrain : IGrainWithStringKey
{
    Task IncreaseQueryLimitAsync(Guid organizationId, long query);
    Task RecordQueryCountAsync(Guid appKeyId, string appId, long query, DateTime dateTime);
}