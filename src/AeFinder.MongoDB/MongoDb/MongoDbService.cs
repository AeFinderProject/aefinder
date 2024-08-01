using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AeFinder.MongoDb;

public class MongoDbService : IMongoDbService
{
    private readonly IMongoClient _mongoClient;
    private readonly MongoOrleansDbOptions _mongoOrleansDbOptions;
    private readonly IMongoDatabase _database;

    public MongoDbService(IMongoClient mongoClient, IOptions<MongoOrleansDbOptions> mongoDbOptions)
    {
        _mongoClient = mongoClient;
        _mongoOrleansDbOptions = mongoDbOptions.Value;
        _database = _mongoClient.GetDatabase(_mongoOrleansDbOptions.DataBase);
    }

    public async Task DeleteRecordsWithPrefixAsync(string collectionName, string idPrefix)
    {
        var collection = _database.GetCollection<BsonDocument>(collectionName);
        var filter = Builders<BsonDocument>.Filter.Regex("_id", new BsonRegularExpression($"^{idPrefix}"));
        var deleteResult = await collection.DeleteManyAsync(filter);

        Console.WriteLine($"Deleted {deleteResult.DeletedCount} records.");
    }
}