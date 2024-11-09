using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AeFinder.ApiTraffic;

public class ApiTrafficWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IApiTrafficProvider _apiTrafficProvider;

    public ApiTrafficWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IApiTrafficProvider apiTrafficProvider, IOptionsSnapshot<ApiTrafficOptions> apiTrafficOptions)
        : base(timer, serviceScopeFactory)
    {
        _apiTrafficProvider = apiTrafficProvider;
        timer.Period = apiTrafficOptions.Value.FlushPeriod * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _apiTrafficProvider.FlushAsync();
    }
}