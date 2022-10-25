using Microsoft.Extensions.DependencyInjection;
using System;

namespace AElfScan.Snapshot.Component.Hosting;

public class SnapshotStorageOptions
{
    public bool UseIndependentEventStorage { get; set; } = false;
        
    public Action<IServiceCollection, string> ConfigureIndependentEventStorage { get; set; } = null;
}