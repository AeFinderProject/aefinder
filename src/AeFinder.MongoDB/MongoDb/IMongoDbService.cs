using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Bson;

namespace AeFinder.MongoDb;

public interface IMongoDbService
{
    Task<List<BsonValue>> QueryRecordIdsWithPrefixAsync(string collectionName, string idPrefix,
        int limitCount);

    Task<long> DeleteRecordsWithIdsAsync(string collectionName, List<BsonValue> recordIdList);
}