using MongoDB.Bson;

namespace AeFinder.BackgroundWorker;

public interface IOrleansDbClearService
{
    Task<List<BsonValue>> QueryRecordIdsWithPrefixAsync(string collectionName, string idPrefix,
        int limitCount);

    Task<long> DeleteRecordsWithIdsAsync(string collectionName, List<BsonValue> recordIdList);
}