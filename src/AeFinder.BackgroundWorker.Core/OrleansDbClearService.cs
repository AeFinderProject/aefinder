using AeFinder.BackgroundWorker.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AeFinder.BackgroundWorker;

public class OrleansDbClearService : IOrleansDbClearService
{
    private readonly IMongoClient _mongoClient;
    private readonly OrleansDataClearOptions _orleansDataClearOptions;
    private readonly IMongoDatabase _database;
    private readonly ILogger<OrleansDbClearService> _logger;

    public OrleansDbClearService(IMongoClient mongoClient, IOptions<OrleansDataClearOptions> orleansDataClearOptions,
        ILogger<OrleansDbClearService> logger)
    {
        _mongoClient = mongoClient;
        _orleansDataClearOptions = orleansDataClearOptions.Value;
        _database = _mongoClient.GetDatabase(_orleansDataClearOptions.DataBase);
        _logger = logger;
    }
    
    public async Task<List<BsonValue>> QueryRecordIdsWithPrefixAsync(string collectionName, string idPrefix,
        int limitCount)
    {
        var resultList = await QueryRecordsWithPrefixAsync(collectionName, idPrefix, limitCount);

        if (resultList == null || resultList.Count == 0)
        {
            _logger.LogInformation("No records found to delete. collectionName:{collectionName} idPrefix:{idPrefix}",
                collectionName, idPrefix);
            return new List<BsonValue>();
        }
        
        var totalSize = resultList.Sum(doc =>
        {
            var size = doc.ToBson().Length;  // Serialize the document to BSON and get its length.
            return size;
        });

        var maxDocumentSize = _orleansDataClearOptions.MongoDbMaxDocumentSize;
        if (totalSize > maxDocumentSize)
        {
            var deviationRatio = _orleansDataClearOptions.ExceedDeviationRatio;
            _logger.LogInformation(
                "The total document size exceeds the limit size {maxDocumentSize}, current total size: {totalSize}, current limit count: {limitCount}, current deviation ratio: {deviationRatio}",
                maxDocumentSize, totalSize, limitCount, deviationRatio);
            double retentionRatio = (double)maxDocumentSize / totalSize;
            limitCount = (int)(limitCount * (retentionRatio - deviationRatio));
            // resultList = await QueryRecordsWithPrefixAsync(collectionName, idPrefix, limitCount);
            _logger.LogInformation("Finally limit count: {limitCount}, recent radio: {retentionRatio}", limitCount,
                retentionRatio);
            resultList = resultList.Take(limitCount).ToList();
        }

        _logger.LogInformation("Found {count} records to delete. collectionName:{collectionName} idPrefix:{idPrefix}",
            resultList.Count, collectionName, idPrefix);
        var ids = resultList.Select(doc => doc["_id"]).ToList();
        return ids;
    }
    
    private async Task<List<BsonDocument>> QueryRecordsWithPrefixAsync(string collectionName, string idPrefix,
        int limitCount)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);

        // Step 1: Query the ids of all records with matching _id prefixes
        var filter = Builders<BsonDocument>.Filter.Regex("_id", new BsonRegularExpression($"^{idPrefix}"));
        var projection = Builders<BsonDocument>.Projection.Include("_id");
        var resultList =
            await collection.Find(filter).Limit(limitCount).Project<BsonDocument>(projection).ToListAsync();

        return resultList;
    }
    
    public async Task<long> DeleteRecordsWithIdsAsync(string collectionName, List<BsonValue> recordIdList)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var deleteFilter = Builders<BsonDocument>.Filter.In("_id", recordIdList);
        var deleteResult = await collection.DeleteManyAsync(deleteFilter);

        _logger.LogInformation($"Deleted {deleteResult.DeletedCount} records. collectionName:{collectionName}");

        return deleteResult.DeletedCount;
    }
}