using System;
using System.Threading;
using System.Threading.Tasks;
using AElfScan.AElf;
using AElfScan.AElf.Dtos;
using AElfScan.AElf.Etos;
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
        _application.Initialize(_serviceProvider);

        BlockEventDataDto eventData = new BlockEventDataDto()
        {
            BlockNumber = 3007,
            IsConfirmed = false
        };
        var appService = _serviceProvider.GetRequiredService<AElfTestAppService>();
        appService.SaveBlock(eventData);
        
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        _application.Shutdown();
        return Task.CompletedTask;
    }
}