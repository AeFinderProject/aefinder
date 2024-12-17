using System.Threading.Tasks;

namespace AeFinder.Market;

public interface IRenewalService
{
    Task<int> GetUserApiQueryFreeCountAsync(string organizationId);
    Task<long> GetUserMonthlyApiQueryAllowanceAsync(string organizationId);
    Task<FullPodResourceDto> GetUserCurrentFullPodResourceAsync(string organizationId, string appId);
}