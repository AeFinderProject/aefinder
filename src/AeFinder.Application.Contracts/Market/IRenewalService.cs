using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IRenewalService
{
    Task<int> GetUserApiQueryFreeCountAsync(string organizationId);
    Task<int> GetUserMonthlyApiQueryAllowanceAsync(string organizationId);
    Task<FullPodResourceLevelDto> GetUserCurrentFullPodResourceAsync(string organizationId, string appId);
}