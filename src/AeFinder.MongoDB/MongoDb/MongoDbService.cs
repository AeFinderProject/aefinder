using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AeFinder.MongoDb;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoOrleansDbOptions _mongoOrleansDbOptions;
    private readonly IMongoDatabase _database;
    private readonly ILogger<MongoDbService> _logger;

    public MongoDbService(IMongoClient mongoClient, IOptions<MongoOrleansDbOptions> mongoDbOptions,
        ILogger<MongoDbService> logger)
    {
        _mongoClient = mongoClient;
        _mongoOrleansDbOptions = mongoDbOptions.Value;
        _database = _mongoClient.GetDatabase(_mongoOrleansDbOptions.DataBase);
        _logger = logger;
    }

    // public async Task DeleteRecordsWithPrefixAsync(string collectionName, string idPrefix)
    // {
    //     var collection = _database.GetCollection<BsonDocument>(collectionName);
    //     var filter = Builders<BsonDocument>.Filter.Regex("_id", new BsonRegularExpression($"^{idPrefix}"));
    //     var deleteResult = await collection.DeleteManyAsync(filter);
    //
    //     Console.WriteLine($"Deleted {deleteResult.DeletedCount} records.");
    // }

    public async Task<List<BsonValue>> QueryRecordIdsWithPrefixAsync(string collectionName, string idPrefix,
        int limitCount)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);

        // Step 1: Query the ids of all records with matching _id prefixes
        var filter = Builders<BsonDocument>.Filter.Regex("_id", new BsonRegularExpression($"^{idPrefix}"));
        var projection = Builders<BsonDocument>.Projection.Include("_id").Exclude("_id");
        var resultList =
            await collection.Find(filter).Limit(limitCount).Project<BsonDocument>(projection).ToListAsync();

        if (resultList == null || resultList.Count == 0)
        {
            _logger.LogInformation("No records found to delete. collectionName:{collectionName} idPrefix:{idPrefix}",
                collectionName, idPrefix);
            return new List<BsonValue>();
        }

        _logger.LogInformation("Found {count} records to delete. collectionName:{collectionName} idPrefix:{idPrefix}",
            resultList.Count, collectionName, idPrefix);
        var ids = resultList.Select(doc => doc["_id"]).ToList();
        return ids;
    }

    public async Task<long> DeleteRecordsWithIdsAsync(string collectionName, List<BsonValue> recordIdList)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var deleteFilter = Builders<BsonDocument>.Filter.In("_id", recordIdList);
        var deleteResult = await collection.DeleteManyAsync(deleteFilter);

        _logger.LogInformation($"Deleted {deleteResult.DeletedCount} records. collectionName:{collectionName}");

        return deleteResult.DeletedCount;
    }
    
    public async Task DeleteRecordsWithPrefixAsync(string collectionName, string idPrefix)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);

        // Step 1: Query the ids of all records with matching _id prefixes
        var filter = Builders<BsonDocument>.Filter.Regex("_id", new BsonRegularExpression($"^{idPrefix}"));
        var projection = Builders<BsonDocument>.Projection.Include("_id").Exclude("_id");
        var ids = await collection.Find(filter).Limit(1000).Project<BsonDocument>(projection).ToListAsync();

        if (ids.Count == 0)
        {
            _logger.LogInformation("No records found to delete. collectionName:{collectionName} idPrefix:{idPrefix}",
                collectionName, idPrefix);
            return;
        }

        _logger.LogInformation("Found {count} records to delete. collectionName:{collectionName} idPrefix:{idPrefix}",
            ids.Count, collectionName, idPrefix);
        
        // Step 2: Batch delete ids using the obtained ID list
        var deleteFilter = Builders<BsonDocument>.Filter.In("_id", ids.Select(id => id["_id"]));
        var deleteResult = await collection.DeleteManyAsync(deleteFilter);

        _logger.LogInformation($"Deleted {deleteResult.DeletedCount} records. collectionName:{collectionName} idPrefix:{idPrefix}");
    }
}