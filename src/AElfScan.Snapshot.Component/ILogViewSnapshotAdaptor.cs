using System.Threading.Tasks;
using Orleans.LogConsistency;

namespace AElfScan.Snapshot.Component;

public interface ILogViewSnapshotAdaptor<TView, TLogEntry> : ILogViewAdaptor<TView, TLogEntry>,
    ILogViewSnapshot<TView, TLogEntry>
    where TView : class, new()
    where TLogEntry : class
{
}

public interface ILogViewSnapshot<TView, TLogEntry> where TView : class, new() where TLogEntry : class
{
    /// <summary>
    /// Interface for setting a flag to determine whether to write a state snapshot 
    /// </summary>
    void SetNeedSnapshotFlag();

    /// <summary>
    /// get the latest storage of snapshot meta data
    /// </summary>
    /// <returns>snapshot's meta data</returns>
    Task<SnapshotStateWithMetaData<TView, TLogEntry>> GetLastSnapshotMetaDataAsync();
}