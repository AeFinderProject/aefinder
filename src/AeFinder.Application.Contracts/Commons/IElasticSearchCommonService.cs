using System.Threading.Tasks;

namespace AeFinder.Commons;

public interface IElasticSearchCommonService
{
    Task DeleteAppIndexAsync(string indexName);
}