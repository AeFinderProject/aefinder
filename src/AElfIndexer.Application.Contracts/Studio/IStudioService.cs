using System.Threading.Tasks;
using AElfIndexer.BlockScan;

namespace AElfIndexer.Studio;

public interface IStudioService
{
    Task<AddOrUpdateAeFinderAppDto> UpdateAeFinderApp(string clientId, AddOrUpdateAeFinderAppInput input);
    Task<ApplyAeFinderAppNameDto> ApplyAeFinderAppName(string clientId, string name);
    Task<AeFinderAppInfoDto> GetAeFinderApp(string clientId, GetAeFinderAppInfoInput input);
}