using Orleans.Runtime;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace AElfScan.Snapshot.Component;

public interface IGrainEventStorage
{
    Task SaveEvents<TEvent>(string grainTypeName, GrainReference grainReference, IEnumerable<TEvent> events, int expectedVersion);

    Task<List<TEvent>> ReadEvents<TEvent>(string grainTypeName, GrainReference grainReference, int start, int count);

    Task<int> EventsCount(string grainTypeName, GrainReference grainReference);
}