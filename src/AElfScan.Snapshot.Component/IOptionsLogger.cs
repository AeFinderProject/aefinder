using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Orleans;

namespace AElfScan.Snapshot.Component;

internal class ClientOptionsLogger : OptionsLogger, ILifecycleParticipant<IClusterClientLifecycle>
{
    private int ClientOptionLoggerLifeCycleRing = int.MinValue;

    public ClientOptionsLogger(ILogger<ClientOptionsLogger> logger, IServiceProvider services)
        : base(logger, services)
    {
    }

    public void Participate(IClusterClientLifecycle lifecycle)
    {
        lifecycle.Subscribe<ClientOptionsLogger>(ClientOptionLoggerLifeCycleRing, this.OnStart);
    }

    public Task OnStart(CancellationToken token)
    {
        this.LogOptions();
        return Task.CompletedTask;
    }
}