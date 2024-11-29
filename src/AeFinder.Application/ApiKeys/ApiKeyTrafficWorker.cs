using System.Threading.Tasks;
using AeFinder.ApiTraffic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AeFinder.ApiKeys;

public class ApiKeyTrafficWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ApiTraffic.IApiTrafficProvider _apiTrafficProvider;

    public ApiKeyTrafficWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ApiTraffic.IApiTrafficProvider apiTrafficProvider, IOptionsSnapshot<ApiTrafficOptions> apiTrafficOptions)
        : base(timer, serviceScopeFactory)
    {
        _apiTrafficProvider = apiTrafficProvider;
        timer.Period = apiTrafficOptions.Value.FlushPeriod * 60 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _apiTrafficProvider.FlushAsync();
    }
}