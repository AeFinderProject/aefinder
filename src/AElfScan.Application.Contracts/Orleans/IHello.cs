using System.Threading.Tasks;
using Orleans;

namespace AElfScan.Orleans;

public interface IHello : IGrainWithIntegerKey
{
    Task<string> SayHello(string greeting);
    Task AddCount();
    Task<int> GetCount();
}