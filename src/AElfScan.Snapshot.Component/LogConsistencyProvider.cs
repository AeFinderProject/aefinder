using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.LogConsistency;
using Orleans.Storage;
using System;using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Orleans.LogConsistency;
using Orleans.Storage;
using System;

namespace AElfScan.Snapshot.Component;

public class LogConsistencyProvider : ILogViewAdaptorFactory
{
    private SnapshotStorageLogConsistencyOptions _options;
    private IGrainEventStorage _eventStorage;

    public bool UsesStorageProvider => true;

    public LogConsistencyProvider(
        SnapshotStorageLogConsistencyOptions options, 
        IGrainEventStorage eventStorage) 
    {
        _options = options;
        _eventStorage = eventStorage;
    }

    public ILogViewSnapshotAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(
        ILogViewAdaptorHost<TLogView, TLogEntry> hostGrain, 
        TLogView initialState, 
        string grainTypeName, 
        IGrainStorage grainStorage, 
        ILogConsistencyProtocolServices services)
        where TLogView : class, new()
        where TLogEntry : class
    {
        return new LogViewAdaptor<TLogView, TLogEntry>(
            hostGrain, 
            initialState, 
            grainStorage,
            grainTypeName,
            services, 
            _options.UseIndependentEventStorage, 
            _eventStorage);
    }
}

public static class LogConsistencyProviderFactory
{
    public static ILogViewAdaptorFactory Create(IServiceProvider services, string name)
    {
        var optionsMonitor = services.GetRequiredService<IOptionsMonitor<SnapshotStorageLogConsistencyOptions>>();
        var options = optionsMonitor.Get(name);

        var eventStorage = options.UseIndependentEventStorage
            ? services.GetRequiredService<IGrainEventStorage>()
            : new NullGrainEventStorage();

        return ActivatorUtilities.CreateInstance<LogConsistencyProvider>(services, options, eventStorage);
    }
}