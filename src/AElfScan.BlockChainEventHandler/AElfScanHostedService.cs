using System;
using System.Threading;
using System.Threading.Tasks;
using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using AElfScan.EventData;
using AElfScan.Orleans;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Volo.Abp;

namespace AElfScan;

public class AElfScanHostedService:IHostedService
{
    private readonly IAbpApplicationWithExternalServiceProvider _application;
    private readonly IServiceProvider _serviceProvider;

    public AElfScanHostedService(
        IAbpApplicationWithExternalServiceProvider application,
        IServiceProvider serviceProvider)
    {
        _application = application;
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        //_application.Initialize(_serviceProvider);
        
        BlockEventDataDto eventDataDto = new BlockEventDataDto()
        {
            BlockNumber = 2005,
            IsConfirmed = false
        };

        var aelfAppService = _serviceProvider.GetRequiredService<AElfTestAppService>();
        aelfAppService.SaveBlock(eventDataDto);

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}