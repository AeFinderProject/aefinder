using System.Threading.Tasks;

namespace AeFinder.MongoDb;

public interface IMongoDbService
{
    Task DeleteRecordsWithPrefixAsync(string collectionName, string idPrefix);
}