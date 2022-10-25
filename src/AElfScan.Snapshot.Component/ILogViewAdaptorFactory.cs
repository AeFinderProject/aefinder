using Orleans.LogConsistency;
using Orleans.Storage;

namespace AElfScan.Snapshot.Component;

public interface ILogViewAdaptorFactory  
{
    /// <summary> Returns true if a storage provider is required for constructing adaptors. </summary>
    bool UsesStorageProvider { get; }

    /// <summary>
    /// Construct a <see cref="LogConsistency.ILogViewAdaptor{TLogView,TLogEntry}"/> to be installed in the given host grain.
    /// </summary>
    Component.ILogViewSnapshotAdaptor<TLogView, TLogEntry> MakeLogViewAdaptor<TLogView, TLogEntry>(
        ILogViewAdaptorHost<TLogView, TLogEntry> hostgrain,
        TLogView initialstate,
        string graintypename,
        IGrainStorage grainStorage,
        ILogConsistencyProtocolServices services)

        where TLogView : class, new()
        where TLogEntry : class;

}