using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace AeFinder.ApiKeys;

public class ApiKeyTrafficWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly IApiKeyTrafficProvider _apiKeyTrafficProvider;

    public ApiKeyTrafficWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        IApiKeyTrafficProvider apiKeyTrafficProvider, IOptionsSnapshot<ApiKeyOptions> apiKeyOptions)
        : base(timer, serviceScopeFactory)
    {
        _apiKeyTrafficProvider = apiKeyTrafficProvider;
        timer.Period = apiKeyOptions.Value.FlushPeriod * 60 * 1000;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _apiKeyTrafficProvider.FlushAsync();
    }
}