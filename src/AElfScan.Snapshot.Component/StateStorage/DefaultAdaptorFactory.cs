using Orleans.Storage;
using Orleans.LogConsistency;

namespace AElfScan.Snapshot.Component.StateStorage;

internal class DefaultAdaptorFactory : ILogViewAdaptorFactory
{
    public bool UsesStorageProvider
    {
        get
        {
            return true;
        }
    }

    public ILogViewSnapshotAdaptor<T, E> MakeLogViewAdaptor<T, E>(ILogViewAdaptorHost<T, E> hostgrain, T initialstate, string graintypename, IGrainStorage grainStorage, ILogConsistencyProtocolServices services)
        where T : class, new() where E : class
    {
        return new LogViewAdaptor<T, E>(hostgrain, initialstate, grainStorage, graintypename, services);
    }

}