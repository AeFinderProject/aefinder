using System.Threading.Tasks;

namespace AeFinder.Commons;

public interface IElasticSearchIndexHelper
{
    Task DeleteAppIndexAsync(string indexName);
}